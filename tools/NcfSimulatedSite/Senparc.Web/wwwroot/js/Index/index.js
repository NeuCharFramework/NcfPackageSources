
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

var ncfI18n = window.ncfI18n || {};

function unopen() {
    alert(ncfI18n.sectionNotOpen || 'This section is not yet open, please stay tuned!');
}

function start(docOpened, xncfName) {
    if (docOpened) {
        return true;
    }

    if (confirm(ncfI18n.installOfflineDocsConfirm || 'You have not installed the offline documentation module. Install now?')) {
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
                    openDocs = confirm(json.message + (ncfI18n.refreshToSeeDocsEntry || '. Refresh this page to see the documentation entry at the top. View documentation now?'));
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
