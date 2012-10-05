# -*- coding: utf-8 -*-
require 'sequel'

db_conn_str = 'postgres://postgres:19820124@localhost/yizhongba'

Sequel.connect(db_conn_str)
