async page => {
    const admin = 'tools/NcfSimulatedSite/Senparc.Areas.Admin';
    const web = 'tools/NcfSimulatedSite/Senparc.Web/wwwroot';
    const origin = 'http://ncf-doc-help.test';
    const failures = [];
    const check = (condition, message) => { if (!condition) throw new Error(message); };
    page.on('pageerror', error => failures.push(error.message));
    await page.route(origin + '/**', async route => {
        const pathname = route.request().url().slice(origin.length).split('?')[0];
        if (pathname.startsWith('/resources/')) {
            await route.fulfill({ path: admin + '/Resources/AdminResource.' + pathname.split('/').pop() + '.resx' });
        } else if (pathname.startsWith('/fonts/')) {
            await route.fulfill({ path: web + '/lib/element-ui_2.13.2' + pathname });
        } else {
            await route.fulfill({ contentType: 'text/html', body: '<!doctype html><html><head></head><body></body></html>' });
        }
    });

    async function mount(culture, pagePath) {
        await page.goto(origin + pagePath);
        await page.evaluate(({ culture, pagePath }) => {
            document.documentElement.lang = culture;
            document.body.innerHTML = `
                <div id="help-test">
                    <header class="el-header" style="height:60px">
                        <div class="header-breadcrumb">NCF</div>
                        <ncf-doc-help class="ncf-doc-help-header" :page-path="pagePath"></ncf-doc-help>
                    </header>
                    <form @submit.prevent="actions++">
                        <button type="button" id="publish" @click="actions++">Publish</button>
                        <ncf-doc-help topic="modules.publish"></ncf-doc-help>
                        <span id="actions">{{ actions }}</span>
                    </form>
                </div>`;
            window.helpTestPagePath = pagePath;
        }, { culture, pagePath });
        await page.addStyleTag({ path: web + '/lib/element-ui_2.13.2/element.css' });
        await page.addStyleTag({ path: admin + '/wwwroot/css/Admin/Shared/layout.css' });
        await page.addStyleTag({ path: admin + '/wwwroot/css/Admin/Shared/NcfDocHelp.css' });
        await page.addScriptTag({ path: web + '/lib/vue/vue.js' });
        await page.addScriptTag({ path: web + '/lib/element-ui_2.13.2/element.js' });
        await page.evaluate(async culture => {
            const resource = await fetch('/resources/' + culture).then(response => response.text());
            const xml = new DOMParser().parseFromString(resource, 'text/xml');
            window.ncfI18n = Object.fromEntries(Array.from(xml.querySelectorAll('data'))
                .map(node => [node.getAttribute('name'), node.querySelector('value').textContent]));
            window.ncfT = key => window.ncfI18n[key] || key;
        }, culture);
        await page.addScriptTag({ path: admin + '/wwwroot/js/Admin/Shared/NcfDocHelpCatalog.js' });
        await page.addScriptTag({ path: admin + '/wwwroot/js/Admin/Shared/NcfDocHelp.js' });
        await page.evaluate(() => {
            window.helpTestApp = new Vue({
                el: '#help-test',
                data: { pagePath: window.helpTestPagePath, actions: 0 }
            });
        });
        check(await page.locator('iframe').count() === 0, 'Documents must not load before a help click');
    }

    async function close() {
        await page.locator('.ncf-doc-help-dialog .el-dialog__footer button').click();
        await page.locator('.ncf-doc-help-dialog').waitFor({ state: 'hidden' });
        check(await page.locator('.ncf-doc-help-frame').count() === 0, 'Closing must release the iframe');
    }

    const results = [];
    for (const culture of ['zh-CN', 'en']) {
        for (const [pagePath, topic] of [
            ['/Admin/Index', 'admin.home'],
            ['/Admin/XncfModule/', 'modules.manage'],
            ['/Admin/MCP/Index', 'mcp.home']
        ]) {
            await mount(culture, pagePath);
            const icon = page.locator('.ncf-doc-help-header');
            await icon.click();
            const expected = await page.evaluate(topic => NcfDocHelp.resolve(topic, document.documentElement.lang), topic);
            check(await page.locator('.ncf-doc-help-frame').getAttribute('src') === expected, 'Incorrect document URL');
            const frame = page.frameLocator('.ncf-doc-help-frame');
            await frame.locator('h1').first().waitFor({ timeout: 25000 });
            check((await frame.locator('h1').first().innerText()).length > 0, 'Live documentation must render inside iframe');
            const external = page.locator('.ncf-doc-help-external');
            check(await external.getAttribute('href') === expected, 'New window link must use the selected document');
            const popupPromise = page.waitForEvent('popup');
            await external.click();
            const popup = await popupPromise;
            await popup.waitForURL(expected);
            await popup.waitForLoadState('domcontentloaded');
            check(popup.url() === expected, 'New window must open the matching locale');
            check(await popup.evaluate(() => window.opener === null), 'New window must not access the Admin opener');
            await popup.close();
            await close();
            check(await icon.evaluate(element => element === document.activeElement), 'Focus must return to help icon');
            results.push(culture + ' ' + topic + ': iframe + new window + focus passed');
        }
        await page.locator('form .ncf-doc-help-button').click();
        const frame = page.frameLocator('.ncf-doc-help-frame');
        const anchor = culture === 'zh-CN' ? '切换-发布-状态' : 'switch-publish-status';
        await frame.locator('[id="' + anchor + '"]').waitFor({ timeout: 25000 });
        check(await page.locator('#actions').innerText() === '0', 'Help must not execute the adjacent action or submit a form');
        check(decodeURIComponent(await page.locator('iframe').getAttribute('src')).endsWith('#' + anchor), 'Chapter anchor must match the locale');
        await page.setViewportSize({ width: 390, height: 844 });
        const box = await page.locator('.ncf-doc-help-dialog').boundingBox();
        check(box.x >= 0 && box.x + box.width <= 391 && box.height <= 844, 'Mobile dialog must fit the viewport');
        await close();
        await page.setViewportSize({ width: 1280, height: 900 });
        results.push(culture + ' modules.publish: live chapter + action isolation + mobile passed');
    }

    for (const culture of ['ja', 'fr', 'es', 'ru']) {
        await mount(culture, '/Admin/MCP');
        await page.locator('.ncf-doc-help-header').click();
        check(await page.locator('iframe').getAttribute('src') === 'https://doc.ncf.pub/MCP/home/index.html', culture + ' must use English documentation');
        check(!(await page.locator('.ncf-doc-help-dialog').innerText()).includes('DocHelp.'), 'Missing help translation');
        await close();
    }
    await mount('en', '/Admin/KnowledgeBase/Index');
    check(await page.locator('.ncf-doc-help-header').count() === 0, 'Unapproved pages must not show a pilot help entry');

    await page.route('https://doc.ncf.pub/**', route => route.abort());
    await page.locator('form .ncf-doc-help-button').click();
    await page.locator('.ncf-doc-help-dialog').waitFor();
    check(await page.locator('.ncf-doc-help-external').isVisible(), 'New-window fallback must remain available when framing fails');
    await close();
    await page.unroute('https://doc.ncf.pub/**');

    await page.clock.install();
    let pendingRoute;
    await page.route('https://doc.ncf.pub/**', route => { pendingRoute = route; });
    const stalledRequest = page.waitForRequest('https://doc.ncf.pub/**');
    await page.locator('form .ncf-doc-help-button').click();
    await stalledRequest;
    await page.clock.fastForward(15001);
    check(await page.locator('.ncf-doc-help-error').isVisible(), 'Timed-out documents must show a visible error');
    await pendingRoute.abort();
    await page.unroute('https://doc.ncf.pub/**');
    await page.locator('.ncf-doc-help-error button').click();
    await page.frameLocator('.ncf-doc-help-frame').locator('h1').first().waitFor({ timeout: 25000 });
    check(await page.locator('.ncf-doc-help-error').count() === 0, 'Retry must clear the timeout error');
    await page.clock.resume();
    await close();

    check(failures.length === 0, failures.join('\n'));
    await page.unroute(origin + '/**');
    return { passed: results, additional: 'All non-Chinese locales, unapproved pages, blocked iframe, timeout and retry passed' };
}
