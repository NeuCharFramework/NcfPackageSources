
//Legacy comment translated to English.
//Legacy comment translated to English.
//    console.log(key, keyPath);

//};
//Legacy comment translated to English.
//    console.log(key, keyPath);
//};

/**

 * Legacy comment translated to English.

 * @param {any} treeNode 2

 * @param {any} result 2

 * English documentation comment.

 */
function getAllChildrenNodes(treeNode, result) {
    if (treeNode.isParent) {
        var childrenNodes = treeNode.children;
        if (childrenNodes) {
            for (var i = 0; i < childrenNodes.length; i++) {
                result.push(childrenNodes[i]);
                result = getAllChildrenNodes(childrenNodes[i], result);
            }
        }
    }
    return result;
}

/**

 * 

 * English documentation comment.

 * English documentation comment.

 * English documentation comment.

 */
async function initMenu(selector, options) {
    return await _initMenuTree(selector || '#tree', '/Admin/Menu/Edit', { handler: 'menu' }, options);
}

/**

 * Legacy comment translated to English.

 * English documentation comment.

 * English documentation comment.

 * @param {any} params requestparameters

 * English documentation comment.

 * English documentation comment.

 */
async function _initMenuTree(selector, url, params, options) {
    var setting = {
        data: {
            simpleData: {
                enable: true,
                pIdKey: "parentId",
                rootPId: null
            },
            key: {
                title: 'id',
                name: 'menuName',
                url: 'xdasdf'
            }
        }
    };
    setting = $.extend(setting, options);
    let responseData = await base.get(url, params);
    if (options.hiddenButton) {
        responseData = responseData.filter((ele, index) => { return ele.isMenu; });
    }
    let treeObj = $.fn.zTree.init($(selector), setting, responseData);
    if (options.IsExpendAll) {
        treeObj.expandAll(true);
    }
    return treeObj;
}


/**

 * Legacy comment translated to English.

 * English documentation comment.

 * English documentation comment.

 * English documentation comment.

 */
function deleteCheck(checkBoxName, formId) {
    var _checkedAll = $("input[name=" + checkBoxName + "]:checked");
    //debugger
    if (_checkedAll.length === 0) {
        //Legacy comment translated to English.
        base.swal.alert('请选择需要删除的内容');
        return false;
    }

    swal({
        text: '确定要删除吗？',
        icon: 'warning',
        buttons: {
            cancel: {
                text: "取消",
                value: null,
                visible: true,
                className: "",
                closeModal: true,
            },
            confirm: {
                text: "确定",
                value: true,
                visible: true,
                className: "",
                closeModal: true
            }
        }
    }).then(v => {
        if (v) {
            $('#' + formId).submit();
        }
    });
}


/**

 * Legacy comment translated to English.

 * English documentation comment. 

 * English documentation comment.

 */
function copyToClipboard(value, toastText) {
    $('#clipboardContainer').val(value);
    $('#clipboardContainer').select();
    toastText = toastText || '复制成功！请使用 Ctrl+V 组合键进行粘贴操作。';
    try {
        document.execCommand("copy"); //Legacy comment translated to English.
        base.swal.toast(toastText);
    } finally {
        //Execute Finally
    }
}


function resizeUrl() {//Legacy comment translated to English.
    let url = window.location.href;
    let obj = {};
    let reg = /[?&][^?&]+=[^?&]+/g;
    let arr = url.match(reg); // return ["?id=123456","&a=b"]
    if (arr) {
        arr.forEach((item) => {
            let tempArr = item.substring(1).split('=');
            let key = tempArr[0];
            let val = tempArr[1];
            obj[key] = decodeURIComponent(val);
        });
    }
    return obj;
}

/**

 * Legacy comment translated to English.

 * English documentation comment.

 * English documentation comment.

 */
function formatCurrency(num) {
    num = num.toString().replace(/\$|\,/g, '');
    if (isNaN(num))
        num = "0";
    sign = num == (num = Math.abs(num));
    num = Math.floor(num * 100 + 0.50000000001);
    cents = num % 100;
    num = Math.floor(num / 100).toString();
    if (cents < 10)
        cents = "0" + cents;
    for (var i = 0; i < Math.floor((num.length - (1 + i)) / 3); i++)
        num = num.substring(0, num.length - (4 * i + 3)) + ',' +
            num.substring(num.length - (4 * i + 3));
    return (((sign) ? '' : '-') + num + '.' + cents);
}