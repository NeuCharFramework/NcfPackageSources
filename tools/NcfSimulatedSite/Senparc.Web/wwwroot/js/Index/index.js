
$(function(){
    $('#qq-code').hover(function () {
        $('#qq-code-img').toggle();
    });

    $('.index-simple-notice').hover(function(){
        $(this).addClass("noticeHover");
    },function(){
        $(this).removeClass("noticeHover");
    });

    $('.start-btn').addClass('normal');
})

function unopen() {
    alert(window.ncfSiteT('Message.SectionComingSoon'));
}

function start(docOpened, xncfName) {
    if (docOpened) {
        return true;
    }

    if (confirm(window.ncfSiteT('Message.InstallOfflineDocsConfirm'))) {
        let openDocs = true;
        $.ajax({
            url: 'Admin/XncfModule/Index?handler=InstallModule&xncfName=' + xncfName,
            method: 'GET',
            async: false,
            success: function (json) {
                let installSuccess = json.success;
                if (!installSuccess) {
                    alert(json.message);
                } else {
                    openDocs = confirm(window.ncfSiteT('Message.OpenOfflineDocsConfirm', json.message));
                    if (!openDocs) {
                        location.reload();
                    }
                }
            }
        });
        return openDocs;
    }
    return false;
}
