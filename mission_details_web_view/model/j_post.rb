# -*- coding: utf-8 -*-
require 'sequel'

class JPost < Sequel::Model(:JPost)
  one_to_many :j_replies, :class => 'JReply', :key => :jpostid, :primary_key => :id
end