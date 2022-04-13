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
Object.defineProperty(exports, "__esModule", { value: true });
var React = require("react");
require("./ChoiceContainer.scss");
var react_northstar_1 = require("@fluentui/react-northstar");
var InputBox_1 = require("../InputBox/InputBox");
var react_i18next_1 = require("react-i18next");
/**
 * <ChoiceContainer> component to add choice input box in creation view
 */
var ChoiceContainer = /** @class */ (function (_super) {
    __extends(ChoiceContainer, _super);
    function ChoiceContainer(props) {
        var _this = _super.call(this, props) || this;
        _this.currentFocus = -1;
        _this.localize = _this.props.t;
        return _this;
    }
    /**
     * method that will add trash icon in input if count of choice is greater than 2 in Poll
     * @param i index of trash icon
     */
    ChoiceContainer.prototype.getDeleteIconProps = function (i) {
        var _this = this;
        if (this.props.options.length > 2) {
            return {
                content: React.createElement(react_northstar_1.TrashCanIcon, { className: "choice-trash-can", outline: true, "aria-hidden": "false", title: this.props.options[i].deleteChoiceLabel, onClick: function () {
                        if (_this.currentFocus == _this.props.options.length - 1) {
                            setTimeout((function () {
                                _this.addButtonRef.focus();
                            }).bind(_this), 0);
                        }
                        _this.props.onDeleteChoice(i);
                    } })
            };
        }
        return null;
    };
    ChoiceContainer.prototype.render = function () {
        var _this = this;
        var items = [];
        var maxOptions = (this.props.limit && this.props.limit > 0) ? this.props.limit : Number.MAX_VALUE;
        var focusOnErrorSet = false;
        var className = "item-content";
        var _loop_1 = function (i) {
            var errorString = this_1.props.optionsError && this_1.props.optionsError.length > i ? this_1.props.optionsError[i] : "";
            if (errorString.length > 0 && this_1.props.focusOnError && !focusOnErrorSet) {
                this_1.currentFocus = i;
                focusOnErrorSet = true;
            }
            items.push(React.createElement("div", { key: "option" + i, className: "choice-item" },
                React.createElement(InputBox_1.InputBox, { ref: function (inputBox) {
                        if (inputBox && i == _this.currentFocus) {
                            inputBox.focus();
                        }
                    }, fluid: true, input: { className: className }, maxLength: this_1.props.maxLength, icon: this_1.getDeleteIconProps(i), showError: errorString.length > 0, errorText: errorString, key: "option" + i, value: this_1.props.options[i].value, placeholder: this_1.props.options[i].choicePlaceholder, onKeyDown: function (e) {
                        if (!e.repeat && (e.keyCode || e.which) == ChoiceContainer.CARRIAGE_RETURN_ASCII_VALUE
                            && _this.props.options.length < maxOptions) {
                            if (i == _this.props.options.length - 1) {
                                _this.props.onAddChoice();
                                _this.currentFocus = _this.props.options.length;
                            }
                            else {
                                _this.currentFocus += 1;
                                _this.forceUpdate();
                            }
                        }
                    }, onFocus: function (e) {
                        _this.currentFocus = i;
                    }, onChange: function (e) {
                        _this.props.onUpdateChoice(i, e.target.value);
                    }, prefixJSX: this_1.props.options[i].choicePrefix })));
        };
        var this_1 = this;
        // if any input is blank while submitting action then there will be entry in optionError
        for (var i = 0; i < (maxOptions > this.props.options.length ? this.props.options.length : maxOptions); i++) {
            _loop_1(i);
        }
        return (React.createElement("div", { className: "choice-container", onBlur: function (e) {
                _this.currentFocus = -1;
            } },
            items,
            this.props.options.length < maxOptions &&
                React.createElement("div", { ref: function (e) {
                        _this.addButtonRef = e;
                    }, className: "add-options", onClick: function (e) {
                        _this.props.onAddChoice();
                        _this.currentFocus = _this.props.options.length;
                    } },
                    React.createElement(react_northstar_1.AddIcon, { className: "plus-icon", outline: true, size: "medium" }),
                    React.createElement(react_northstar_1.Text, { size: "medium", content: this.localize("PollAddChoice") }))));
    };
    ChoiceContainer.CARRIAGE_RETURN_ASCII_VALUE = 13;
    return ChoiceContainer;
}(React.Component));
//export default ChoiceContainer;
exports.default = react_i18next_1.withTranslation()(ChoiceContainer);
//# sourceMappingURL=ChoiceContainer.js.map