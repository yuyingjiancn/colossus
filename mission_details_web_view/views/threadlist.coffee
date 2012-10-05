$ ->
  $("#startDatetime").kendoDateTimePicker({format: "yyyy-MM-dd HH:mm:ss", culture: "zh-CN"});
  $("#endDatetime").kendoDateTimePicker({format: "yyyy-MM-dd HH:mm:ss", culture: "zh-CN"});
  $('#btn_goto').click ->
    start = $('#startDatetime').val().split(' ');
    start_url = start[0].split('-').join('/') + '/' + start[1].split(':').join('/');
    end = $('#endDatetime').val().split(' ');
    end_url = end[0].split('-').join('/') + '/' + end[1].split(':').join('/');
    if start_url > end_url
      t = end_url
      end_url = start_url
      start_url = t
    window.location = "/threadlist/between/" + start_url + "/to/" + end_url + "/limit/50/0"
    false
  return