colossus(百度贴吧爬虫)
爬虫部分使用visual studio 2012开发，c# 4.0编写（基于.net framework 4.0 full）。
========
说明
========
vs2012打开项目编译可执行程序。
config.json是配置文件，内部的参数简单介绍如下：
exec_type: 程序执行类型，init代表初始化数据库，crawl代表抓数据。
name: 百度贴吧的名称，fw=后面的url encode的字符串。
crawl_start_thread_list_page_index: 抓取数据的开始页的index，
http://tieba.baidu.com/f?kw=%BA%A3%C4%FE%D2%BB%D6%D0&pn=300 pn=后面的300就是index。
thread_list_crawl_max_page: 抓取数据页面的限制，最多抓多少，和上面参数一样，是个index，
如果为0则不限制抓取的页面数一直抓到结束为止。
post_list_crawl_max_page: 同上某个thread最多抓取的post数，0为无限制。
reply_list_crawl_max_page: 同上post的reply抓取上限，0为无限制。
same_thread_count_max：供1+n次使用，当抓取到的thread有多少相同的时候，我们认为已经没有
新的更新的帖子了，我设置了200。
connection_string: 数据库连接配置字符串，使用前要修改。你要用其它数据库也可以，自己修
改源码。

第一次抓取时间比较久，我是扔在服务器上运行的，60w的帖子数一个贴吧大概是半天多的时间。
以后可以设成计划任务，6个小时执行一次，确保更新的内容都被抓下来。
1+n次抓取的算法没有做优化，因为我要保持数据库和贴吧同步，还要做删除帖子的对应数据库
的删除，所以更新的帖子都是post和reply都要再爬一遍的。
这个爬虫是单线程的，我用ruby写过，用nodejs也写过。nodejs异步就相当于多线程但是抓取效果
也不理想，http请求会断，会抛异常。ruby是因为速度太慢，nokogiri这个gem对html的分析不好。

问题
========
问题1：放入github源代码管理的时候没有把packages目录也上传（packages目录比较大），
所以在git签出项目或者直接下载zip包第1次打开解决方案的时候会有问题：项目无法加载。
问题1解决：
1.vs2012打开以后在解决方案上->右键->启用nuget程序包还原。
2.重新加载项目。（此时nuget会自动下载依赖的packages）。
如果想要手动还原也可以的，到codeplex下载nuget.exe最新版，然后转到项目根目录执行：
nuget.exe install Colossus.Crawler\packages.config -o packages
不过解决方案项目比较多的情况，一个一个执行你会不会觉得累-_-

鸣谢
========
鸣谢以下开源类库的贡献者的劳动：
HtmlAgilityPack: html页面的分析全靠这个库了。
Json.net: json格式数据分析转换工具，部分数据获取全靠它的分析。
NHibernate: 非常强大的orm工具，有了它免去了写sql的痛苦。
            还要感谢Nhibernate社区的贡献者提供的Linq支持，查询是如此简单。
Fluent NHibernate: 自从有了你，NHibernate就不需要再使用xml来配置映射了，
                   如此简单方便。
Npgsql: 用postgresql数据库，没有你怎么行。

