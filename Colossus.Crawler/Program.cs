using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NHibernate;
using FluentNHibernate;
using FluentNHibernate.Mapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using NHibernate.Linq;
using NHibernate.Type;
using HtmlAgilityPack;


namespace Colossus.Crawler
{
    public class JThread {
        public virtual int JId { get; set; }
        public virtual long Id { get; set; }
        public virtual long ReplyNum { get; set; }
        public virtual long IsBakan { get; set; }
        public virtual string VId { get; set; }
        public virtual string Title { get; set; }
        public virtual string Author { get; set; }
        public virtual bool ZhiDing { get; set; }
        public virtual bool Ding { get; set; }
        public virtual bool Jing { get; set; }
        public virtual DateTime Pub { get; set; }
        public virtual DateTime CreateAt { get; set; }
        public virtual DateTime UpdateAt { get; set; }
    }

    public class JThreadMap : ClassMap<JThread> {
        public JThreadMap() {
            Id(j => j.JId);
            Map(j => j.Id);
            Map(j => j.ReplyNum);
            Map(j => j.IsBakan);
            Map(j => j.VId);
            Map(j => j.Title);
            Map(j => j.Author);
            Map(j => j.ZhiDing);
            Map(j => j.Ding);
            Map(j => j.Jing);
            Map(j => j.Pub);
            Map(j => j.CreateAt);
            Map(j => j.UpdateAt);
        }
    }

    public class JPost
    {
        public virtual int JId { get; set; }
        public virtual long Id { get; set; }
        public virtual long JThreadId { get; set; }
        public virtual int Floor { get; set; }
        public virtual string Author { get; set; }
        public virtual string Content { get; set; }
        public virtual int ReplyNumber { get; set; }
        public virtual int ReplyPageCount { get; set; }
        public virtual DateTime Pub { get; set; }
        public virtual DateTime CreateAt { get; set; }
        public virtual DateTime UpdateAt { get; set; }
    }

    public class JPostMap : ClassMap<JPost>
    {
        public JPostMap() {
            Id(j => j.JId);
            Map(j => j.Id);
            Map(j => j.JThreadId);
            Map(j => j.Floor);
            Map(j => j.Author);
            Map(j => j.Content).CustomSqlType("text");//postgres的text类型可以存储任意长度的字符串
            Map(j => j.ReplyNumber);
            Map(j => j.ReplyPageCount);
            Map(j => j.Pub);
            Map(j => j.CreateAt);
            Map(j => j.UpdateAt);
        }
    }

    public class JReply
    {
        public virtual int JId { get; set; }
        public virtual long Id { get; set; }
        public virtual long JThreadId { get; set; }
        public virtual long JPostId { get; set; }
        public virtual string Author { get; set; }
        public virtual string Content { get; set; }
        public virtual DateTime Pub { get; set; }
        public virtual DateTime CreateAt { get; set; }
        public virtual DateTime UpdateAt { get; set; } 
    }

    public class JReplyMap : ClassMap<JReply>
    {
        public JReplyMap()
        {
            Id(j => j.JId);
            Map(j => j.Id);
            Map(j => j.JThreadId);
            Map(j => j.JPostId);
            Map(j => j.Author);
            Map(j => j.Content).CustomSqlType("text");//postgres的text类型可以存储任意长度的字符串
            Map(j => j.Pub);
            Map(j => j.CreateAt);
            Map(j => j.UpdateAt);
        }
    }

    public class CrawlMission
    {
        public virtual int Id { get; set; }
        public virtual DateTime Start { get; set; }
        public virtual DateTime Finish { get; set; }
    }

    public class CrawlMissionMap : ClassMap<CrawlMission>
    {
        public CrawlMissionMap()
        {
            Id(x => x.Id);
            Map(x => x.Start);
            Map(x => x.Finish);
        }
    }

    public class ErrorLog
    {
        public virtual int Id { get; set; }
        public virtual int CrawlMissionId { get; set; }
        public virtual string LogType { get; set; }
        public virtual string Log { get; set; }
        public virtual DateTime At { get; set; }
    }

    public class ErrorLogMap : ClassMap<ErrorLog>
    {
        public ErrorLogMap() {
            Id(x => x.Id);
            Map(x => x.CrawlMissionId);
            Map(x => x.LogType);
            Map(x => x.Log);
            Map(x => x.At);

        }
    }

    class Program
    {
        static ISessionFactory sessionFactory = null;
        static int missionId = 0; //每次执行这个程序在数据库记录的抓取任务的Id

        private static ISessionFactory CreateSessionFactory()
        {
            return Fluently.Configure()
                .Database(PostgreSQLConfiguration.Standard.ConnectionString(connectionStr))
                .Mappings(x => x.FluentMappings
                    .AddFromAssemblyOf<CrawlMission>()
                    .AddFromAssemblyOf<ErrorLogMap>()
                    .AddFromAssemblyOf<JReply>()
                    .AddFromAssemblyOf<JPostMap>()
                    .AddFromAssemblyOf<JThreadMap>())
                //.ExposeConfiguration(BuildSchema)
                .BuildSessionFactory();
        }
        private static void BuildSchema(Configuration config)
        {
            //如果数据库结构没创建 用下面的方法创建
            new SchemaExport(config)
              .Create(false, true);
        }
        
        static string execType = "crawl";
        static int threadListCrawlMaxPage = 0;
        static int postListCrawlMaxPage = 0;
        static int replyListCrawlMaxPage = 0;
        static string tiebaName = "";
        static int crawlStartIndex = 0;
        static string connectionStr = "";
        static int sameThreadCountMax = 200;
        static int sameThreadCount = 0;

        static string GetUrltoHtml(string Url)
        {
            var html = string.Empty;
            try
            {
                System.Net.WebRequest wReq = System.Net.WebRequest.Create(Url);
                System.Net.WebResponse wResp = wReq.GetResponse();
                System.IO.Stream respStream = wResp.GetResponseStream();

                using (System.IO.StreamReader reader = new System.IO.StreamReader(respStream, Encoding.GetEncoding("gbk")))
                {
                    html = reader.ReadToEnd();
                }
            }
            catch
            {
                html = string.Empty; //不管是何种情况只要页面未获取则页面的html被设备string.Empty
            }
            return html; //html为空代表页面获取失败
        }

        /// <summary>
        /// 根据xpath抓取最后一页的a link的页码
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="queryPath"></param>
        /// <param name="getNumFromLinkHref"></param>
        /// <returns></returns>
        static int getNextPageNum(HtmlDocument doc, string queryPath, Func<string,int> getNumFromLinkHref) {
            var pageNum = 0;
            var aNextPage = doc.DocumentNode.SelectSingleNode(queryPath);          
            if (aNextPage != null)
            {
                var linkHref = aNextPage.Attributes["href"].Value;
                pageNum = getNumFromLinkHref(linkHref);
            }
            return pageNum;
        }

        static void ThreadCrawl(int threadListPageNo=0){
            Console.WriteLine(threadListPageNo);
            if (sameThreadCount > sameThreadCountMax) { //用于>=1次查询的时候 如果有sameThreadCountMax个thread相同的时候认为已经没有新的帖子更新过了
                Console.WriteLine("已经没有新的更新了，程序退出。。。");
                return;
            }
            string html = GetUrltoHtml("http://tieba.baidu.com/f?kw=" + tiebaName + "&pn=" + threadListPageNo);

            if (string.Empty == html)
            {
                //若返回的html为空，则表示这个页面的html抓取失败，记录数据库并重新从该页开始递归抓取thread list。
                using (var session = sessionFactory.OpenSession())
                {
                    session.SaveOrUpdate(new ErrorLog { 
                        CrawlMissionId = missionId,
                        LogType="thread list crawl http get error",
                        Log = String.Format("page number: [{0}]", threadListPageNo),
                        At = DateTime.Now
                    });
                }
                ThreadCrawl(threadListPageNo);
                return;
            }
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            HtmlNodeCollection  nli = doc.DocumentNode.SelectNodes("//li[@class=\"j_thread_list\"]");
            if (nli == null) {
                using (var session = sessionFactory.OpenSession())
                {
                    session.SaveOrUpdate(new ErrorLog
                    {
                        CrawlMissionId = missionId,
                        LogType = "thread list crawl empty thread list err",
                        Log = String.Format("page number: [{0}]", threadListPageNo),
                        At = DateTime.Now
                    });
                }
                return;
            }

            foreach (var n in nli)
            {
                var thread = JsonConvert.DeserializeObject<Hashtable>(n.Attributes["data-field"].Value.Replace("&quot;", "\""));
                thread["title"] = n.SelectSingleNode(".//a[@class=\"j_th_tit\"]").InnerText;
                thread["author"] = "匿名吧友";
                if (n.SelectSingleNode(".//div[@class=\"threadlist_author\"]/span/a") != null)
                    thread["author"] = n.SelectSingleNode(".//div[@class=\"threadlist_author\"]/span/a").InnerText;
                thread["zhi_ding"] = false;
                thread["ding"] = false;
                thread["jing"] = false;
                if (n.SelectNodes(".//div[@class=\"threadlist_title\"]/img") != null)
                {
                    foreach (var img in n.SelectNodes(".//div[@class=\"threadlist_title\"]/img"))
                    {
                        if (img.Attributes["src"].Value.Contains("zding.gif")) thread["zhi_ding"] = true;
                        if (img.Attributes["src"].Value.Contains("ding.gif")) thread["ding"] = true;
                        if (img.Attributes["src"].Value.Contains("jing.gif")) thread["jing"] = true;
                    }
                }
                thread["create_at"] = DateTime.Now;
                thread["update_at"] = DateTime.Now;

                var jthread = new JThread
                {
                    Id = (long)thread["id"],
                    ReplyNum = (long)thread["reply_num"],
                    IsBakan = (long)thread["is_bakan"],
                    VId = thread["vid"] == null ? string.Empty : (string)thread["vid"],
                    Title = (string)thread["title"],
                    Author = (string)thread["author"],
                    ZhiDing = (bool)thread["zhi_ding"],
                    Ding = (bool)thread["ding"],
                    Jing = (bool)thread["jing"],
                    CreateAt = (DateTime)thread["create_at"],
                    UpdateAt = (DateTime)thread["update_at"]
                };
                var needReCrawlPost = false;
                using (var session = sessionFactory.OpenSession())
                {
                    JThread jthreadInDB;
                    try
                    {
                        jthreadInDB = session.Query<JThread>().Single<JThread>(j => j.Id == jthread.Id);
                    }
                    catch (InvalidOperationException)
                    {
                        jthreadInDB = null;
                    }
                    catch (ArgumentNullException) {
                        jthreadInDB = null;
                    }
                    if (jthreadInDB == null)
                    {
                        session.Save(jthread);
                        needReCrawlPost = true;
                        sameThreadCount = 0;
                    }
                    else {
                        //thread被回复或者thread的置顶 顶 精状态被修改 则更新thread的更新时间
                        if (jthreadInDB.ReplyNum != jthread.ReplyNum ||
                            jthreadInDB.ZhiDing != jthread.ZhiDing ||
                            jthreadInDB.Ding != jthread.Ding ||
                            jthreadInDB.Jing != jthread.Jing)
                        {
                            jthreadInDB.UpdateAt = DateTime.Now;
                            jthreadInDB.ReplyNum = jthread.ReplyNum; //这里做的比较简单 如果有新的post或者新的reply 那么这个值不是最新的 大致同步就好-_-
                            jthreadInDB.ZhiDing = jthread.ZhiDing;
                            jthreadInDB.Ding = jthread.Ding;
                            jthreadInDB.Jing = jthread.Jing;
                            session.Update(jthreadInDB);
                            session.Flush();
                        }
                        if (jthreadInDB.ReplyNum != jthread.ReplyNum)
                        {
                            needReCrawlPost = true;
                            sameThreadCount = 0;
                        }
                        else {
                            sameThreadCount++;
                        }             
                    }
                }
                if (needReCrawlPost) {
                    var postIdsArr = new List<long>();
                    PostCrawl(jthread.Id, ref postIdsArr, 1);
                    using (var session = sessionFactory.OpenSession())
                    {
                        var posts = session.Query<JPost>().Where(x => x.JThreadId == jthread.Id).ToList();
                        var postIdsStoreArr = new List<long>();
                        foreach (var p in posts)
                        {
                            postIdsStoreArr.Add(p.Id);
                        }
                        var delIds = postIdsStoreArr.Except(postIdsArr).ToList<long>();

                        if (delIds.Count > 0)
                        {
                            var delPosts = session.Query<JPost>().Where(x => delIds.Contains(x.Id)).ToList();
                            foreach (var r in delPosts)
                            {
                                session.Delete(r);
                            }
                            session.Flush();
                        }
                    }
                }
            }

            var nextPageNum = getNextPageNum(doc, "//a[@class=\"next\"]", href => {
                return int.Parse(href.Replace("/f?kw=" + tiebaName + "&pn=", ""));
            });
            if (nextPageNum != 0) {
                if (threadListCrawlMaxPage == 0 || threadListCrawlMaxPage >= nextPageNum)
                {
                    ThreadCrawl(nextPageNum);
                }
            }
        }

        static void PostCrawl(long threadId, ref List<long> parentThreadPostIdArr, int postListPageNo = 1)
        {
            string html = GetUrltoHtml("http://tieba.baidu.com/p/" + threadId + "?pn=" + postListPageNo);

            if (string.Empty == html)
            {
                //若返回的html为空，则表示这个页面的html抓取失败，记录数据库并重新从该页开始递归抓取thread list。
                using (var session = sessionFactory.OpenSession())
                {
                    session.SaveOrUpdate(new ErrorLog
                    {
                        CrawlMissionId = missionId,
                        LogType = "post list http get error",
                        Log = String.Format("thread id: [{0}];page number: [{1}]", threadId, postListPageNo),
                        At = DateTime.Now
                    });
                }
                PostCrawl(threadId, ref parentThreadPostIdArr, postListPageNo);
                return;
            }
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            HtmlNodeCollection nDiv = doc.DocumentNode.SelectNodes("//div[contains(concat(\" \", @class, \" \"), \"l_post\")]");
            if (nDiv == null) {
                using (var session = sessionFactory.OpenSession())
                {
                    session.SaveOrUpdate(new ErrorLog
                    {
                        CrawlMissionId = missionId,
                        LogType = "post list crawl empty post list error",
                        Log = String.Format("thread id: [{0}];page number: [{1}]", threadId, postListPageNo),
                        At = DateTime.Now
                    });
                }
                var nextPageNum = getNextPageNum(doc, "//ul[@class=\"l_posts_num\"]//a[text()=\"下一页\"]", href =>
                {
                    return int.Parse(href.Replace(string.Format("/p/{0}?pn=", threadId), ""));
                });
                if (nextPageNum != 0)
                {
                    if (postListCrawlMaxPage == 0 || nextPageNum <= postListCrawlMaxPage)
                    {
                        PostCrawl(threadId, ref parentThreadPostIdArr, nextPageNum);
                    }
                }
                return;
                
            }
            
            foreach (var n in nDiv)
            {
                var jpost = new JPost();
                var postInfo = JsonConvert.DeserializeObject<Hashtable>(
                    n.SelectSingleNode(".//div[contains(concat(\" \", @class, \" \"), \"p_post\")]")
                    .Attributes["data-field"].Value.Replace("&quot;", "\""));
                var post = postInfo["content"].As<JObject>();
                //post = postInfo["content"].As<JObject>();
                jpost.Id = post.Value<long>("id");
                jpost.Floor = post.Value<int>("floor");
                jpost.Pub = post.Value<DateTime>("date");
                jpost.JThreadId = threadId;
                try
                {
                    jpost.Author = postInfo["author"].As<JObject>().Value<string>("name");
                }
                catch {
                    jpost.Author = "匿名吧友";
                }
                jpost.Content = n.SelectSingleNode(".//div[@class=\"d_post_content\"]").InnerHtml;

                Hashtable df = new Hashtable();
                try
                {
                    df = JsonConvert.DeserializeObject<Hashtable>(n.SelectSingleNode(".//li[contains(concat(\" \", @class, \" \"), \"lzl_li_pager\")]").Attributes["data-field"].Value
                        .Replace("total_num", "'total_num'").Replace("total_page", "'total_page'").Replace("'", "\""));
                    if (df["total_num"].ToString() == string.Empty)
                    {
                        jpost.ReplyNumber = 0;
                    }
                    else {
                        jpost.ReplyNumber = int.Parse(df["total_num"].ToString());
                    }
                    if (df["total_page"].ToString() == string.Empty)
                    {
                        jpost.ReplyPageCount = 0;
                    }
                    else
                    {
                        jpost.ReplyPageCount = int.Parse(df["total_page"].ToString());
                    }
                }
                catch {
                    jpost.ReplyNumber = 0;
                    jpost.ReplyPageCount = 0;
                }
                jpost.CreateAt = DateTime.Now;
                jpost.UpdateAt = DateTime.Now;

                parentThreadPostIdArr.Add(jpost.Id);

                //如果是一楼 获取发布时间 更新thread的发布时间-_-
                if (jpost.Floor == 1) {
                    using (var session = sessionFactory.OpenSession()) {
                        var t = session.Query<JThread>().Single(x => x.Id == threadId);
                        t.Pub = jpost.Pub;
                        session.Update(t);
                        session.Flush();
                    }
                }
                //Console.WriteLine(JsonConvert.SerializeObject(jpost));
                bool needReCrawlReplies = false;
                using (var session = sessionFactory.OpenSession())
                {
                    JPost jpostDB;
                    try
                    {
                        jpostDB = session.Query<JPost>().Single<JPost>(j => j.Id == jpost.Id);
                    }
                    catch (InvalidOperationException)
                    {
                        jpostDB = null;
                    }
                    catch (ArgumentNullException)
                    {
                        jpostDB = null;
                    }
                    if (jpostDB == null)
                    {
                        session.Save(jpost);
                        if (jpost.ReplyNumber > 0)
                        {
                            needReCrawlReplies = true;
                        }
                    }
                    else
                    {
                        //post被回复则更新post的reply数量和post的更新时间
                        if (jpostDB.ReplyNumber != jpost.ReplyNumber)
                        {
                            jpostDB.UpdateAt = DateTime.Now;
                            jpostDB.ReplyNumber = jpost.ReplyNumber; //这里做的比较简单 如果有新的reply 那么这个值不是最新的 大致同步就好-_-
                            jpostDB.ReplyPageCount = jpost.ReplyPageCount;
                            session.Update(jpostDB);
                            session.Flush();
                            needReCrawlReplies = true;
                        }
                    }
                }
                if (needReCrawlReplies)
                {
                    var replyIdsArr = new List<long>();
                    ReplyCrawl(threadId, jpost.Id, ref replyIdsArr);
                    using (var session = sessionFactory.OpenSession())
                    {
                        var replies = session.Query<JReply>().Where(x => x.JPostId == jpost.Id).ToList();
                        var replyIdsStoreArr = new List<long>();
                        foreach (var r in replies)
                        {
                            replyIdsStoreArr.Add(r.Id);
                        }
                        var delIds = replyIdsStoreArr.Except(replyIdsArr).ToList<long>();

                        if (delIds.Count > 0)
                        {
                            var delReplies = session.Query<JReply>().Where(x => delIds.Contains(x.Id)).ToList();
                            foreach (var r in delReplies)
                            {
                                session.Delete(r);
                            }
                            session.Flush();
                        }
                    }
                }
            }
            var nextPageNum2 = getNextPageNum(doc, "//ul[@class=\"l_posts_num\"]//a[text()=\"下一页\"]", href =>
            {
                return int.Parse(href.Replace(string.Format("/p/{0}?pn=", threadId), ""));
            });
            if (nextPageNum2 != 0)
            {
                if (postListCrawlMaxPage == 0 || nextPageNum2 <= postListCrawlMaxPage)
                {
                    PostCrawl(threadId, ref parentThreadPostIdArr, nextPageNum2);
                }
            }
        }

        static void ReplyCrawl(long threadId, long postId, ref List<long> parentReplyIdsArr, int replyListPageNo = 1)
        {
            string html = GetUrltoHtml(string.Format("http://tieba.baidu.com/p/comment?tid={0}&pid={1}&pn={2}", threadId, postId, replyListPageNo));

            if (string.Empty == html)
            {
                //若返回的html为空，则表示这个页面的html抓取失败，记录数据库并重新从该页开始递归抓取thread list。
                using (var session = sessionFactory.OpenSession())
                {
                    session.SaveOrUpdate(new ErrorLog
                    {
                        CrawlMissionId = missionId,
                        LogType = "reply list http get error",
                        Log = String.Format("thread id: [{0}];post id: [{1}];page number: [{2}]", threadId, postId, replyListPageNo),
                        At = DateTime.Now
                    });
                }
                ReplyCrawl(threadId, postId, ref parentReplyIdsArr, replyListPageNo);
                return;
            }

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            HtmlNodeCollection nDiv = doc.DocumentNode.SelectNodes("//li[contains(concat(\" \", @class, \" \"), \"lzl_single_post\")]");
            if (nDiv == null) {
                using (var session = sessionFactory.OpenSession())
                {
                    session.SaveOrUpdate(new ErrorLog
                    {
                        CrawlMissionId = missionId,
                        LogType = "reply list crawl reply list empty error",
                        Log = String.Format("thread id: [{0}];post id: [{1}];page number: [{2}]", threadId, postId, replyListPageNo),
                        At = DateTime.Now
                    });
                }
                var nextPageNum = getNextPageNum(doc, "//p[contains(concat(\" \", @class, \" \"), \"j_pager\")]/a[text()=\"下一页\"]", href =>
                {
                    return int.Parse(href.Replace("#", ""));
                });
                if (nextPageNum != 0)
                {
                    if (replyListCrawlMaxPage == 0 || nextPageNum <= replyListCrawlMaxPage)
                    {
                        ReplyCrawl(threadId, postId, ref parentReplyIdsArr, nextPageNum);
                    }
                }
                return;
            }

            foreach (var n in nDiv)
            {
                var jreply = new JReply();
                var replyInfo = JsonConvert.DeserializeObject<Hashtable>(n.Attributes["data-field"].Value
                    .Replace("spid", "'spid'").Replace("user_name", "'user_name'").Replace("portrait", "'portrait'").Replace("'", "\""));
                jreply.Id = long.Parse(replyInfo["spid"].ToString());
                jreply.JThreadId = threadId;
                jreply.JPostId = postId;
                jreply.Author = (string)replyInfo["user_name"];
                jreply.Content = n.SelectSingleNode(".//span[@class=\"lzl_content_main\"]").InnerHtml;
                jreply.Pub = Convert.ToDateTime(n.SelectSingleNode(".//span[@class=\"lzl_time\"]").InnerText.ToString());
                jreply.CreateAt = DateTime.Now;
                jreply.UpdateAt = DateTime.Now;

                parentReplyIdsArr.Add(jreply.Id);

                using (var session = sessionFactory.OpenSession())
                {
                    JReply jreplyDB;
                    try
                    {
                        jreplyDB = session.Query<JReply>().Single<JReply>(j => j.Id == jreply.Id);
                    }
                    catch (InvalidOperationException)
                    {
                        jreplyDB = null;
                    }
                    catch (ArgumentNullException)
                    {
                        jreplyDB = null;
                    }
                    if (jreplyDB == null)
                    {
                        session.Save(jreply);
                    }
                }
            }
            var nextPageNum2 = getNextPageNum(doc, "//p[contains(concat(\" \", @class, \" \"), \"j_pager\")]/a[text()=\"下一页\"]", href =>
            {
                return int.Parse(href.Replace("#", ""));
            });
            if (nextPageNum2 != 0)
            {
                if (replyListCrawlMaxPage == 0 || nextPageNum2 <= replyListCrawlMaxPage)
                {
                    ReplyCrawl(threadId, postId, ref parentReplyIdsArr, nextPageNum2);
                }
            }
        }

        static void Main(string[] args)
        {
            try
            {
                var configJson = ReadFile("config.json", Encoding.UTF8, true);
                var configHash = JsonConvert.DeserializeObject<Hashtable>(configJson);
                execType = (string)configHash["exec_type"];
                tiebaName = (string)configHash["name"];
                crawlStartIndex = int.Parse(configHash["crawl_start_thread_list_page_index"].ToString());
                threadListCrawlMaxPage = int.Parse(configHash["thread_list_crawl_max_page"].ToString());
                postListCrawlMaxPage = int.Parse(configHash["post_list_crawl_max_page"].ToString());
                replyListCrawlMaxPage = int.Parse(configHash["reply_list_crawl_max_page"].ToString());
                sameThreadCountMax = int.Parse(configHash["same_thread_count_max"].ToString());
                connectionStr = configHash["connection_string"].ToString();
            }
            catch {
                Console.WriteLine("配置文件未找到...程序退出。");
                return;
            }


            if (execType == "init") //初始化数据库-_-
            {
                Fluently.Configure()
                .Database(PostgreSQLConfiguration.Standard.ConnectionString(connectionStr))
                .Mappings(x => x.FluentMappings
                    .AddFromAssemblyOf<CrawlMission>()
                    .AddFromAssemblyOf<ErrorLogMap>()
                    .AddFromAssemblyOf<JReply>()
                    .AddFromAssemblyOf<JPostMap>()
                    .AddFromAssemblyOf<JThreadMap>())
                .ExposeConfiguration(BuildSchema)
                .BuildSessionFactory();
            }
            else if (execType == "crawl") //执行爬虫操作^_^ :P
            {
                sessionFactory = CreateSessionFactory();
                using (var session = sessionFactory.OpenSession())
                {
                    missionId = (int)session.Save(new CrawlMission { Start = DateTime.Now });
                }
                ThreadCrawl(crawlStartIndex);
                using (var session = sessionFactory.OpenSession())
                {
                    var crawlMission = session.Query<CrawlMission>().Single(cm => cm.Id == missionId);
                    session.Update(crawlMission);
                    session.Flush();
                }
            }
            else { 
                //todo custom
            }

            
            
        }

        static string ReadFile(string path, Encoding enc, bool detectEncodingFromByteOrderMarks)
        {
            string fileContent = string.Empty;
            StreamReader sr;
            using (sr = new StreamReader(path, enc, detectEncodingFromByteOrderMarks))
            {
                fileContent = sr.ReadToEnd();
            }
            return fileContent;
        }
    }
}
