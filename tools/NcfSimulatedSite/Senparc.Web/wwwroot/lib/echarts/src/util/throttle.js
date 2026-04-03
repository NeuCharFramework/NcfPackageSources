define(function () {

    var lib = {};

    var ORIGIN_METHOD = '\0__throttleOriginMethod';
    var RATE = '\0__throttleRate';

    /**
     * Frequency control When the return function is called continuously, the execution frequency of fn is limited to how many times it is executed.
     * For example, common effects:
     * notifyWhenChangesStop
     * When called frequently, only the last execution is guaranteed.
     * Match: trailing: true; debounce: true
     * notifyAtFixRate
     * When called frequently, execute according to regular heartbeat
     * Match: trailing: true; debounce: false
     * Notice:
     * When updating the view based on the model, you can use throttle.
     * But when updating the model based on the view, avoid using this delayed update method.
     * Because this may cause synchronization problems between the model and the server.
     *
     * @public
     * @param {(Function|Array.<Function>)} fn The function to be called
     * If fn is an array, it means that multiple functions can be throttled.
     * They share the same timer.
     * @param {number} delay delay time, in milliseconds
     * @param {bool} whether trailing guarantees the execution of the last trigger
     * true: Indicates that the last call is guaranteed to trigger execution.
     * But it is impossible to execute immediately after any call, and there will always be a delay.
     * false: Indicates that the last call is not guaranteed to trigger execution.
     * But as long as the interval is greater than delay, the call will be executed immediately.
     * @param {bool} debounce throttling
     * true: means: when called frequently (the interval is less than delay), it will not be executed at all.
     * false: means: when called frequently (interval less than delay), execute according to regular heartbeat
     * @return {(Function|Array.<Function>)} actually calls the function.
     * When the input fn is an array, the return value is also an array.
     * Each item is Function.
     */
    lib.throttle = function (fn, delay, trailing, debounce) {

        var currCall = (new Date()).getTime();
        var lastCall = 0;
        var lastExec = 0;
        var timer = null;
        var diff;
        var scope;
        var args;
        var isSingle = typeof fn === 'function';
        delay = delay || 0;

        if (isSingle) {
            return createCallback();
        }
        else {
            var ret = [];
            for (var i = 0; i < fn.length; i++) {
                ret[i] = createCallback(i);
            }
            return ret;
        }

        function createCallback(index) {

            function exec() {
                lastExec = (new Date()).getTime();
                timer = null;
                (isSingle ? fn : fn[index]).apply(scope, args || []);
            }

            var cb = function () {
                currCall = (new Date()).getTime();
                scope = this;
                args = arguments;
                diff = currCall - (debounce ? lastCall : lastExec) - delay;

                clearTimeout(timer);

                if (debounce) {
                    if (trailing) {
                        timer = setTimeout(exec, delay);
                    }
                    else if (diff >= 0) {
                        exec();
                    }
                }
                else {
                    if (diff >= 0) {
                        exec();
                    }
                    else if (trailing) {
                        timer = setTimeout(exec, -diff);
                    }
                }

                lastCall = currCall;
            };

            /**
             * Clear throttle.
             * @public
             */
            cb.clear = function () {
                if (timer) {
                    clearTimeout(timer);
                    timer = null;
                }
            };

            return cb;
        }
    };

    /**
     * Executed at a certain frequency, the last call will always be executed
     *
     * @public
     */
    lib.fixRate = function (fn, delay) {
        return delay != null
            ? lib.throttle(fn, delay, true, false)
            : fn;
    };

    /**
     * It will not be executed until it is called infrequently. The last call will always be executed.
     *
     * @public
     */
    lib.debounce = function (fn, delay) {
        return delay != null
             ? lib.throttle(fn, delay, true, true)
             : fn;
    };


    /**
     * Create throttle method or update throttle rate.
     *
     * @example
     * ComponentView.prototype.render = function () {
     *     ...
     *     throttle.createOrUpdate(
     *         this,
     *         '_dispatchAction',
     *         this.model.get('throttle'),
     *         'fixRate'
     *     );
     * };
     * ComponentView.prototype.remove = function () {
     *     throttle.clear(this, '_dispatchAction');
     * };
     * ComponentView.prototype.dispose = function () {
     *     throttle.clear(this, '_dispatchAction');
     * };
     *
     * @public
     * @param {Object} obj
     * @param {string} fnAttr
     * @param {number} rate
     * @param {string} throttleType 'fixRate' or 'debounce'
     */
    lib.createOrUpdate = function (obj, fnAttr, rate, throttleType) {
        var fn = obj[fnAttr];

        if (!fn || rate == null || !throttleType) {
            return;
        }

        var originFn = fn[ORIGIN_METHOD] || fn;
        var lastRate = fn[RATE];

        if (lastRate !== rate) {
            fn = obj[fnAttr] = lib[throttleType](originFn, rate);
            fn[ORIGIN_METHOD] = originFn;
            fn[RATE] = rate;
        }
    };

    /**
     * Clear throttle. Example see throttle.createOrUpdate.
     *
     * @public
     * @param {Object} obj
     * @param {string} fnAttr
     */
    lib.clear = function (obj, fnAttr) {
        var fn = obj[fnAttr];
        if (fn && fn[ORIGIN_METHOD]) {
            obj[fnAttr] = fn[ORIGIN_METHOD];
        }
    };

    return lib;
});
