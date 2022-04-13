"use strict";
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
var __extends = (this && this.__extends) || (function () {
    var extendStatics = function (d, b) {
        extendStatics = Object.setPrototypeOf ||
            ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
            function (d, b) { for (var p in b) if (Object.prototype.hasOwnProperty.call(b, p)) d[p] = b[p]; };
        return extendStatics(d, b);
    };
    return function (d, b) {
        if (typeof b !== "function" && b !== null)
            throw new TypeError("Class extends value " + String(b) + " is not a constructor or null");
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();
var __assign = (this && this.__assign) || function () {
    __assign = Object.assign || function(t) {
        for (var s, i = 1, n = arguments.length; i < n; i++) {
            s = arguments[i];
            for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p))
                t[p] = s[p];
        }
        return t;
    };
    return __assign.apply(this, arguments);
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.InputBox = void 0;
var React = require("react");
require("./InputBox.scss");
var react_northstar_1 = require("@fluentui/react-northstar");
var Utils_1 = require("../Utils");
var errorIcon = {
    content: React.createElement(react_northstar_1.ExclamationCircleIcon, { className: "settings-icon", outline: true, color: "brand" })
};
var RenderAs;
(function (RenderAs) {
    RenderAs[RenderAs["Input"] = 0] = "Input";
    RenderAs[RenderAs["TextArea"] = 1] = "TextArea";
})(RenderAs || (RenderAs = {}));
/**
 * Input component supporting multiline input also
 */
var InputBox = /** @class */ (function (_super) {
    __extends(InputBox, _super);
    function InputBox() {
        var _this = _super !== null && _super.apply(this, arguments) || this;
        _this.renderAs = RenderAs.Input;
        _this.incomingInputRef = null;
        _this.bottomBorderWidth = -1;
        return _this;
    }
    InputBox.prototype.componentDidUpdate = function () {
        this.autoAdjustHeight();
    };
    InputBox.prototype.componentDidMount = function () {
        var _this = this;
        if (this.renderAs == RenderAs.TextArea && (!Utils_1.Utils.isEmpty(this.props.value) || !Utils_1.Utils.isEmpty(this.props.defaultValue))) {
            // Updating height only in case when there is some text in input box becasue if there is no text then rows=1 will show only 1 line.
            // There might be some senario in which element is not completely painted to get their scroll height. Refer https://stackoverflow.com/questions/26556436/react-after-render-code
            // In such cases the height of input box become wrong(looks very large or very small), which usaully occurs on very first load.
            // To solve this, trying to adjust the height after window has resize which supposed to be called once load and rendering is done.
            this.autoAdjustHeight();
            window.addEventListener("resize", function () {
                _this.autoAdjustHeight();
            });
        }
    };
    InputBox.prototype.render = function () {
        if (this.props.multiline) {
            this.renderAs = RenderAs.TextArea;
        }
        return (React.createElement(react_northstar_1.Flex, { column: true },
            (this.props.showError && !Utils_1.Utils.isEmpty(this.props.errorText)) &&
                React.createElement(react_northstar_1.Text, { align: "end", error: true }, this.props.errorText),
            this.props.prefixJSX ? this.getInputItem() : this.getInput()));
    };
    InputBox.prototype.focus = function () {
        if (this.inputElement) {
            this.inputElement.focus();
        }
    };
    InputBox.prototype.getInputItem = function () {
        return (React.createElement(react_northstar_1.Flex, { gap: "gap.smaller" },
            this.props.prefixJSX,
            this.getInput()));
    };
    InputBox.prototype.getInput = function () {
        var _this = this;
        return (React.createElement(react_northstar_1.Input, __assign({}, this.getInputProps(), { onChange: function (event, data) {
                _this.autoAdjustHeight();
                if (_this.props.onChange) {
                    _this.props.onChange(event, data);
                }
            }, onClick: this.props.disabled ? null : function (event) {
                // Adjusting height if by any reason wrong height get applied in componentDidMount.
                _this.autoAdjustHeight();
                if (_this.props.onClick) {
                    _this.props.onClick(event);
                }
            } })));
    };
    /**
     * Method to adjust height in case of multiline input
     */
    InputBox.prototype.autoAdjustHeight = function () {
        if (this.renderAs == RenderAs.TextArea) {
            this.inputElement.style.height = "";
            if (this.bottomBorderWidth == -1) {
                this.bottomBorderWidth = parseFloat(getComputedStyle(this.inputElement).getPropertyValue("border-bottom-width"));
            }
            this.inputElement.style.height = this.inputElement.scrollHeight + this.bottomBorderWidth + "px";
        }
    };
    InputBox.prototype.getInputProps = function () {
        var _this = this;
        var icon = this.props.icon || (this.props.showError ? errorIcon : null);
        this.incomingInputRef = this.props.inputRef;
        var inputRef = function (input) {
            _this.inputElement = input;
            if (_this.incomingInputRef) {
                if (typeof _this.incomingInputRef === "function") {
                    _this.incomingInputRef(input);
                }
                else if (typeof _this.incomingInputRef === "object") {
                    _this.incomingInputRef.current = input;
                }
            }
        };
        var input = this.props.input;
        if (this.renderAs == RenderAs.TextArea) {
            input = __assign(__assign({}, input), { as: "textarea", rows: 1 });
        }
        var classNames = ["multiline-input-box"];
        if (this.props.className) {
            classNames.push(this.props.className);
        }
        if (this.props.showError) {
            classNames.push("invalid");
        }
        return __assign(__assign({}, __assign(__assign({}, this.props), { multiline: undefined })), { className: classNames.join(" "), icon: icon, inputRef: inputRef, input: input });
    };
    return InputBox;
}(React.Component));
exports.InputBox = InputBox;
//# sourceMappingURL=InputBox.js.map