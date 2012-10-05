# -*- coding: utf-8 -*-
require 'sinatra'
require 'haml'
require 'coffee-script'

require_relative 'db/init'
require_relative 'model/j_crawl_mission'
require_relative 'model/j_thread'
require_relative 'model/j_post'
require_relative 'model/j_reply'

set :port, 8080

get '/' do
  redirect '/crawlmissionlist/50/0'
end

get '/crawlmissionlist/:limit/:offset' do |l, o|
  @page_limit = l.to_i
  @page_offset = o.to_i
  @count = CrawlMission.all.count
  @page_num = @count / @page_limit
  @page_num = @page_num + 1 if (@count % @page_limit) != 0
  @crawl_missions = CrawlMission.reverse_order(:id).limit(l, o)
  haml :crawlmission
end

get '/threadlist/between/:sy/:smm/:sd/:sh/:sm/:ss/to/:ey/:emm/:ed/:eh/:em/:es/limit/:limit/:offset' do |sy, smm, sd, sh, sm, ss, ey, emm, ed, eh, em, es, l, o|
  @start_time_url = "#{sy}/#{smm}/#{sd}/#{sh}/#{sm}/#{ss}"
  @end_time_url = "#{ey}/#{emm}/#{ed}/#{eh}/#{em}/#{es}"
  @page_limit = l.to_i
  @page_offset = o.to_i
  @start_time = Time.mktime(sy, smm, sd, sh, sm, ss).strftime('%Y-%m-%d %H:%M:%S')
  @end_time = Time.mktime(ey, emm, ed, eh, em, es).strftime('%Y-%m-%d %H:%M:%S')
  @count = JThread.where(:updateat => "#{@start_time}".."#{@end_time}").count
  @page_num = @count / @page_limit
  @page_num = @page_num + 1 if (@count % @page_limit) != 0
  @threads = JThread.where(:updateat => "#{@start_time}".."#{@end_time}").limit(l, o)
  haml :threadlist
end
get '/threadlist.js' do
  coffee :threadlist
end

get '/threadid/:id/postlist/between/:sy/:smm/:sd/:sh/:sm/:ss/to/:ey/:emm/:ed/:eh/:em/:es/limit/:limit/:offset' do |id, sy, smm, sd, sh, sm, ss, ey, emm, ed, eh, em, es, l, o|
  @start_time_url = "#{sy}/#{smm}/#{sd}/#{sh}/#{sm}/#{ss}"
  @end_time_url = "#{ey}/#{emm}/#{ed}/#{eh}/#{em}/#{es}"
  @page_limit = l.to_i
  @page_offset = o.to_i
  @start_time = Time.mktime(sy, smm, sd, sh, sm, ss).strftime('%Y-%m-%d %H:%M:%S')
  @end_time = Time.mktime(ey, emm, ed, eh, em, es).strftime('%Y-%m-%d %H:%M:%S')
  @count = JPost.where(:updateat => "#{@start_time}".."#{@end_time}", :jthreadid => id).count
  @page_num = @count / @page_limit
  @page_num = @page_num + 1 if (@count % @page_limit) != 0
  @thread = JThread.find(:id => id)
  @posts = JPost.where(:updateat => "#{@start_time}".."#{@end_time}", :jthreadid => id).order(:floor).limit(l, o)
  @JReply = JReply
  haml :postlist
end
get '/postlist.js' do
  coffee :postlist
end