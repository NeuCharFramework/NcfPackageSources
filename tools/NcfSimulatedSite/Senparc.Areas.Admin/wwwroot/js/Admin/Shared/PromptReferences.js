(function (window) {
    'use strict';
    const trackers = new WeakMap();
    const scores = Vue.observable({});
    const pending = new Map();
    const t = key => window.ncfT('PromptReference.' + key);
    const api = '/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.';

    async function request(method, data, post) {
        const response = post
            ? await service.post(api + method, data, { customAlert: true })
            : await service.get(api + method, { params: data, customAlert: true });
        if (!response || !response.data || !response.data.success || !response.data.data) {
            throw new Error(response && response.data && response.data.errorMessage || t('Unavailable'));
        }
        const info = response.data.data;
        Vue.set(scores, info.promptItemId, info);
        if (post) window.dispatchEvent(new CustomEvent('ncf-prompt-score-changed', { detail: info }));
        return info;
    }

    function load(id) {
        if (!pending.has(id)) {
            pending.set(id, request('GetReferenceInfoAsync', { id }).finally(() => pending.delete(id)));
        }
        return pending.get(id);
    }

    function updateRanges(previous, value, references, edit) {
        if (previous === value && (!edit || edit.start === edit.end)) return references.slice();
        let start = 0;
        while (start < previous.length && start < value.length && previous[start] === value[start]) start++;
        let end = previous.length;
        let newEnd = value.length;
        while (end > start && newEnd > start && previous[end - 1] === value[newEnd - 1]) { end--; newEnd--; }
        // beforeinput identifies the actual edited occurrence when identical text appears more than once.
        if (edit && edit.start <= start && edit.end >= edit.start &&
            previous.slice(0, edit.start) === value.slice(0, edit.start) &&
            previous.slice(edit.end) === value.slice(value.length - (previous.length - edit.end))) {
            start = edit.start;
            end = edit.end;
        }
        const delta = value.length - previous.length;
        return references.flatMap(reference => {
            const referenceEnd = reference.start + reference.length;
            if (end <= reference.start) return [{ ...reference, start: reference.start + delta }];
            if (start >= referenceEnd) return [{ ...reference }];
            return [];
        });
    }

    function publish(state, references) {
        state.references = references;
        state.view.references = references;
        state.input.dispatchEvent(new CustomEvent('prompt-references-change', {
            bubbles: true, detail: references.map(reference => ({ ...reference }))
        }));
    }

    function sync(input) {
        const state = trackers.get(input);
        if (!state || state.value === input.value) return;
        // Programmatic replacement has no edit intent: never infer a reference from matching text.
        state.value = input.value;
        publish(state, []);
    }

    window.NcfPromptReferences = {
        updateRanges,
        scores,
        attach(input, host) {
            if (!input || trackers.has(input)) return;
            const mount = document.createElement('div');
            mount.className = 'ncf-prompt-reference-labels';
            host.appendChild(mount);
            const state = { input, value: input.value, references: [], edit: null };
            state.view = new Vue({
                el: mount, data: { references: [] },
                methods: {
                    remove(index) { publish(state, state.references.filter((_, position) => position !== index)); },
                    locate(reference) { input.focus(); input.setSelectionRange(reference.start, reference.start + reference.length); }
                },
                template: `<div class="ncf-prompt-reference-labels">
                    <prompt-reference-tag v-for="(reference, index) in references"
                        :key="reference.promptItemId + ':' + reference.start" :reference="reference"
                        removable @remove="remove(index)" @locate="locate(reference)"></prompt-reference-tag>
                </div>`
            });
            state.before = function (event) {
                state.edit = { start: input.selectionStart, end: input.selectionEnd, type: event.inputType };
            };
            state.changed = function () {
                if (state.edit && state.edit.start === state.edit.end && input.value.length < state.value.length) {
                    const deleted = state.value.length - input.value.length;
                    if (/Backward$/.test(state.edit.type)) state.edit.start -= deleted;
                    else if (/Forward$/.test(state.edit.type)) state.edit.end += deleted;
                }
                const references = updateRanges(state.value, input.value, state.references, state.edit);
                state.value = input.value;
                state.edit = null;
                publish(state, references);
            };
            input.addEventListener('beforeinput', state.before);
            // Track native edits before Vue's input listener can render and synchronize the directive.
            input.addEventListener('input', state.changed, true);
            trackers.set(input, state);
        },
        sync,
        capture(input) {
            sync(input);
            const state = trackers.get(input);
            return state ? state.references.map(reference => ({ ...reference })) : [];
        },
        restore(input, references) {
            const state = trackers.get(input);
            if (!state) return;
            if (references != null && (!Array.isArray(references) || references.length > 32)) {
                throw new Error(t('InvalidReference'));
            }
            let end = 0;
            for (const reference of references || []) {
                if (!reference || !Number.isInteger(reference.start) || reference.start < end ||
                    !Number.isInteger(reference.length) || reference.length <= 0 ||
                    reference.start + reference.length > input.value.length ||
                    !Number.isInteger(reference.promptItemId) || reference.promptItemId <= 0 ||
                    typeof reference.promptCode !== 'string' || !reference.promptCode.trim()) {
                    throw new Error(t('InvalidReference'));
                }
                end = reference.start + reference.length;
            }
            state.value = input.value;
            publish(state, (references || []).map(reference => ({ ...reference })));
        },
        inserted(input, previous, start, end, item) {
            const state = trackers.get(input);
            if (!state) return;
            const references = updateRanges(previous, input.value, state.references, { start, end });
            references.push({ promptItemId: item.id, promptCode: item.fullVersion, start, length: item.content.length });
            state.value = input.value;
            state.edit = null;
            publish(state, references.sort((a, b) => a.start - b.start));
        },
        detach(input) {
            const state = trackers.get(input);
            if (!state) return;
            input.removeEventListener('beforeinput', state.before);
            input.removeEventListener('input', state.changed, true);
            state.view.$destroy();
            state.view.$el.remove();
            trackers.delete(input);
        }
    };

    Vue.component('prompt-reference-tag', {
        props: { reference: { type: Object, required: true }, removable: Boolean, aiScore: Function },
        data() { return { scores, error: '', visible: false, busy: false, score: null }; },
        computed: {
            info() { return this.scores[this.reference.promptItemId]; },
            label() {
                const score = this.info && this.info.currentScore;
                return this.reference.promptCode + ' · ' +
                    (Number.isFinite(score) && score >= 0 ? score.toFixed(1) : t('Unrated'));
            }
        },
        mounted() { this.refresh(); },
        methods: {
            t,
            async refresh() {
                try { await load(this.reference.promptItemId); this.error = ''; }
                catch (error) { this.error = error.message || t('Unavailable'); }
            },
            async command(command) {
                if (command === 'view') { window.NcfPromptRange.openPrompt(this.reference.promptItemId); return; }
                if (command === 'remove') { this.$emit('remove'); return; }
                if (command === 'locate') { this.$emit('locate'); return; }
                this.visible = true;
                await this.refresh();
                this.score = this.info && this.info.currentScore >= 0 ? this.info.currentScore : null;
            },
            async save() {
                if (!Number.isFinite(this.score) || this.score < 0 || this.score > 10 ||
                    Math.round(this.score * 10) !== this.score * 10) {
                    this.error = t('InvalidScore'); return;
                }
                this.busy = true;
                try {
                    await request('ScoreReferenceAsync', { id: this.reference.promptItemId, score: this.score }, true);
                    this.error = ''; this.visible = false;
                } catch (error) { this.error = error.message || t('Unavailable'); }
                finally { this.busy = false; }
            },
            async autoScore() {
                try { await this.$confirm(t('AIConfirm'), t('AIScore'), { type: 'warning' }); }
                catch (action) { if (action === 'cancel' || action === 'close') return; throw action; }
                this.busy = true;
                try {
                    const info = await this.aiScore(this.reference.promptItemId);
                    Vue.set(scores, info.promptItemId, info);
                    window.dispatchEvent(new CustomEvent('ncf-prompt-score-changed', { detail: info }));
                    this.score = info.currentScore; this.error = '';
                } catch (error) { this.error = error.message || t('Unavailable'); }
                finally { this.busy = false; }
            }
        },
        template: `<span class="ncf-prompt-reference-tag">
            <el-dropdown trigger="click" @command="command">
                <button type="button" class="ncf-prompt-reference-pill" :title="error || reference.promptCode">
                    {{ label }} <i class="el-icon-arrow-down"></i><span v-if="error"> !</span>
                </button>
                <el-dropdown-menu slot="dropdown">
                    <el-dropdown-item command="score">{{ t('Score') }}</el-dropdown-item>
                    <el-dropdown-item command="view">{{ t('View') }}</el-dropdown-item>
                    <el-dropdown-item v-if="removable" command="locate">{{ t('Locate') }}</el-dropdown-item>
                    <el-dropdown-item v-if="removable" command="remove">{{ t('Remove') }}</el-dropdown-item>
                </el-dropdown-menu>
            </el-dropdown>
            <el-dialog v-if="visible" :visible.sync="visible" :title="reference.promptCode"
                append-to-body width="min(460px, 94vw)" :close-on-click-modal="false"
                :show-close="!busy" :close-on-press-escape="!busy">
                <p>{{ t('LastScoreWins') }}</p>
                <p v-if="error" role="alert" class="ncf-prompt-selector-error">{{ error }}</p>
                <el-input-number v-model="score" :min="0" :max="10" :precision="1" :step="0.1"
                    :disabled="busy" :aria-label="t('Score')"></el-input-number>
                <p v-if="!info || !info.hasBenchmark || !aiScore">{{ t('AIUnavailable') }}</p>
                <div slot="footer">
                    <el-button :disabled="busy" @click="visible=false">{{ t('Cancel') }}</el-button>
                    <el-button v-if="aiScore" :disabled="busy || !info || !info.hasBenchmark"
                        @click="autoScore">{{ t('AIScore') }}</el-button>
                    <el-button type="primary" :loading="busy" @click="save">{{ t('Save') }}</el-button>
                </div>
            </el-dialog>
        </span>`
    });

    Vue.component('prompt-reference-content', {
        props: { content: String, references: Array, aiScore: Function },
        computed: {
            parts() {
                let start = 0;
                const parts = [];
                for (const reference of this.references || []) {
                    if (reference.start > start) parts.push({ text: this.content.slice(start, reference.start) });
                    parts.push({ text: this.content.slice(reference.start, reference.start + reference.length), reference });
                    start = reference.start + reference.length;
                }
                if (start < this.content.length) parts.push({ text: this.content.slice(start) });
                return parts;
            }
        },
        template: `<div class="ncf-prompt-reference-content" data-i18n-skip><template v-for="(part, index) in parts"><span
            v-if="!part.reference" :key="index">{{ part.text }}</span><span v-else :key="index"
            class="ncf-prompt-reference-segment"><prompt-reference-tag :reference="part.reference"
            :ai-score="aiScore"></prompt-reference-tag><span>{{ part.text }}</span></span></template></div>`
    });
})(window);
