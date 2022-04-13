"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.Utils = void 0;
var Utils;
(function (Utils) {
    /**
     * Method to check whether the obj param is empty or not
     * @param obj
     */
    function isEmpty(obj) {
        if (obj == undefined || obj == null) {
            return true;
        }
        var isEmpty = false; // isEmpty will be false if obj type is number or boolean so not adding a check for that
        if (typeof obj === "string") {
            isEmpty = (obj.trim().length == 0);
        }
        else if (Array.isArray(obj)) {
            isEmpty = (obj.length == 0);
        }
        else if (typeof obj === "object") {
            isEmpty = (JSON.stringify(obj) == "{}");
        }
        return isEmpty;
    }
    Utils.isEmpty = isEmpty;
})(Utils = exports.Utils || (exports.Utils = {}));
//# sourceMappingURL=Utils.js.map