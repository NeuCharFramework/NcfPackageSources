'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const admin = path.resolve(__dirname, '../../../Senparc.Areas.Admin');
const components = {};
const Vue = function (options) {
    return Object.assign(options.data, { $el: { remove() {} }, $destroy() {} });
};
Vue.observable = value => value;
Vue.set = (object, key, value) => { object[key] = value; };
Vue.component = (name, component) => { components[name] = component; };
const window = { ncfT: key => key, dispatchEvent() {} };
const sandbox = {
    window, Vue, document: { createElement: () => ({}) },
    CustomEvent: function (type, options) { this.type = type; this.detail = options.detail; },
    service: {}
};
vm.runInNewContext(fs.readFileSync(path.join(admin, 'wwwroot/js/Admin/Shared/PromptReferences.js'), 'utf8'), sandbox);
const refs = window.NcfPromptReferences;
const clone = value => JSON.parse(JSON.stringify(value));
const reference = (id, start, length) => ({ promptItemId: id, promptCode: `code-${id}`, start, length });
const original = 'aaPROMPTbbOTHERcc';
const both = [reference(1, 2, 6), reference(2, 10, 5)];
assert.deepEqual(clone(refs.updateRanges(original, 'x' + original, both)), [reference(1, 3, 6), reference(2, 11, 5)]);
assert.deepEqual(clone(refs.updateRanges(original, 'aaPROxMPTbbOTHERcc', both)), [reference(2, 11, 5)]);
assert.deepEqual(clone(refs.updateRanges(original, 'aaPROMPTbbOTHxERcc', both)), [reference(1, 2, 6)]);
assert.deepEqual(clone(refs.updateRanges(original, 'aaPROMPTXbbOTHERcc', both)), [reference(1, 2, 6), reference(2, 11, 5)]);
assert.deepEqual(clone(refs.updateRanges(original, 'aaxcc', both)), []);
assert.deepEqual(clone(refs.updateRanges('aaaa', 'aaa', [reference(1, 0, 2), reference(2, 2, 2)], { start: 0, end: 1 })),
    [reference(2, 1, 2)], 'Identical text must use the edited occurrence, not a guessed suffix.');
const input = {
    value: '', listeners: {}, events: [],
    addEventListener(name, callback) { this.listeners[name] = callback; },
    removeEventListener(name) { delete this.listeners[name]; },
    dispatchEvent(event) { this.events.push(event); }
};
refs.attach(input, { appendChild() {} });
input.value = 'PROMPT';
refs.inserted(input, '', 0, 0, { id: 1, fullVersion: 'code-1', content: 'PROMPT' });
input.listeners.input();
assert.deepEqual(clone(refs.capture(input)), [reference(1, 0, 6)], 'Input dispatch must not double-shift inserted references.');
refs.inserted(input, 'PROMPT', 0, 6, { id: 1, fullVersion: 'code-1', content: 'PROMPT' });
assert.deepEqual(clone(refs.capture(input)), [reference(1, 0, 6)], 'Replacing a selected prompt with itself must not duplicate its reference.');
input.selectionStart = 3; input.selectionEnd = 3;
input.listeners.beforeinput({ inputType: 'insertText' });
input.value = 'PROxMPT'; input.listeners.input();
assert.equal(refs.capture(input).length, 0);
input.value = 'PROMPT'; input.listeners.input();
assert.equal(refs.capture(input).length, 0, 'Undo must not infer a cancelled reference.');
refs.restore(input, [reference(1, 0, 6)]);
input.value = 'other'; refs.sync(input);
assert.equal(refs.capture(input).length, 0, 'Programmatic replacement cancels annotations.');
assert.throws(() => refs.restore(input, [reference(1, 0, 6)]), /InvalidReference/);
assert.throws(() => refs.restore(input, [null]), /InvalidReference/);
assert.throws(() => refs.restore(input, [reference(1, 0, 1.5)]), /InvalidReference/);
assert.throws(() => refs.restore(input, {}), /InvalidReference/);
refs.detach(input);
assert.equal(Object.keys(input.listeners).length, 0);
assert.ok(components['prompt-reference-content'].template.includes('{{ part.text }}'), 'History content must render literal text.');
assert.ok(!components['prompt-reference-content'].template.includes('v-html'));
const entity = fs.readFileSync(path.join(admin, 'Domain/Models/DatabaseModel/AdminChatMessage.cs'), 'utf8');
assert.match(entity, /PromptReferencesJson/);
const chat = fs.readFileSync(path.join(admin, 'wwwroot/js/Admin/Pages/AdminChat/Chat.js'), 'utf8');
assert.match(chat, /content: messageContent,\s+promptReferences/);
console.log('Prompt reference tracking tests passed.');
