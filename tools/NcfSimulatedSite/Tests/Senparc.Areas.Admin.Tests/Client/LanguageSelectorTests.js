const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');

const siteRoot = path.resolve(__dirname, '../../../');

const expectedLabels = [
    '简体中文',
    'English',
    '日本語',
    'Français',
    'Español',
    'Русский'
];

function read(relativePath) {
    return fs.readFileSync(path.join(siteRoot, relativePath), 'utf8');
}

function languageOptionLabels(source) {
    return [...source.matchAll(/<option value="[^"]+"[^>]*>([^<]+)<\/option>/g)]
        .map(match => match[1]);
}

test('language selector labels remain fixed across admin and shared layouts', () => {
    const files = [
        'Senparc.Areas.Admin/Areas/Admin/Pages/Shared/_Layout_Vue.cshtml',
        'Senparc.Areas.Admin/Areas/Admin/Pages/Login.cshtml',
        'Senparc.Web/Pages/Shared/_Layout.cshtml'
    ];

    for (const file of files) {
        const source = read(file);
        assert.deepEqual(languageOptionLabels(source), expectedLabels, file);
        assert.doesNotMatch(source, /<option value="[^"]+"[^>]*>@(?:AR|SR)\[/, file);
        assert.match(source, /<select[^>]*data-i18n-skip[^>]*>[\s\S]*<option value="zh-CN"/, file);
    }
});
