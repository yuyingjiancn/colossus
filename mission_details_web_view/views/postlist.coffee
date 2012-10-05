$ ->
  $('#btn_remove_protected_images').click ->
    $('img.BDE_Image').each ->
      $(this).after('<a href="' + $(this).attr('src') + '" target="_blank">view protected image -_-</a>').remove()
      return
    false
  return