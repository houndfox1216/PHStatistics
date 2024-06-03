$(document).ready(() => {
    setTimeout(() => AOS.init(), 1000);

    $('.fixed-link').fadeOut();

    $(window).scroll(() => {
        if ($(this).scrollTop() > 200) $('.fixed-link').fadeIn();
        else $('.fixed-link').fadeOut();
    });

    $('.goTop').click(function () {
        if (navigator.userAgent.indexOf('Safari') != -1 && navigator.userAgent.indexOf('Chrome') == -1) {
            $('html, body').stop().animate({ scrollTop: 0 }, { duration: 800 });
            return false;
        } else {
            $('html, body').stop().animate({ scrollTop: 0 }, 0);
            return false;
        }
    });

    let lineclamps = document.getElementsByClassName('line-clamp2');
    for (var i = 0; i < lineclamps.length; i++) $clamp(lineclamps[i], { clamp: 2 });
    lineclamps = document.getElementsByClassName('line-clamp3');
    for (var i = 0; i < lineclamps.length; i++) $clamp(lineclamps[i], { clamp: 3 });
    lineclamps = document.getElementsByClassName('line-clamp4');
    for (var i = 0; i < lineclamps.length; i++) $clamp(lineclamps[i], { clamp: 4 });
    lineclamps = document.getElementsByClassName('line-clamp5');
    for (var i = 0; i < lineclamps.length; i++) $clamp(lineclamps[i], { clamp: 5 });

    //function addClass(cId) {
    //    alert(cId);
    //    //$.ajax({
    //    //    url: '@Url.Action("AddClass", "StudentPopulation")',
    //    //    cache: false,
    //    //    data: { $("#formPopulation").serialize(), coursesId: cId },
    //    //    dataType: 'json',
    //    //    type: 'POST',
    //    //    success: function (data) {
    //    //        $('.item-quantity, .btn-minus, .btn-pluus').attr('disabled', false);
    //    //        countSelected();
    //    //    },
    //    //    error: function () {

    //    //    }
    //    //})
    //}
});


