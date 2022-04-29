// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import * as React from "react";
import "./ChoiceContainer.scss";
import { Text, Checkbox, ShorthandValue, AddIcon, BoxProps, TrashCanIcon, Input, AcceptIcon, Status } from "@fluentui/react-northstar";
import { InputBox } from "../InputBox/InputBox";
import { TFunction } from "i18next";
import { withTranslation, WithTranslation } from "react-i18next";

export interface IChoiceContainerOption {
    value: string;
    checked: boolean;
    choicePlaceholder: string;
    deleteChoiceLabel: string;
}

export interface IChoiceContainerProps extends WithTranslation {
    title?: string;
    options: IChoiceContainerOption[];
    optionsError?: string[];
    limit?: number;
    maxLength?: number;
    multiselect?: boolean;
    quizMode: boolean;
    focusOnError?: boolean;
    inputClassName?: string;
    onUpdateChoice?: (i, value) => void;
    onDeleteChoice?: (i) => void;
    onAddChoice?: () => void;
    onItemChecked?: (i, value: boolean) => void;
}

/**
 * <ChoiceContainer> component to add choice input box in creation view
 */
class ChoiceContainer extends React.Component<IChoiceContainerProps> {
    public static readonly CARRIAGE_RETURN_ASCII_VALUE = 13;
    private currentFocus: number = -1;
    private addButtonRef: HTMLElement;
    readonly localize: TFunction;

    constructor(props: IChoiceContainerProps) {
        super(props);
        this.localize = this.props.t;        
    }

    /**
     * method that will add trash icon in input if count of choice is greater than 1 in Poll
     * @param i index of trash icon
     */
    getDeleteIconProps(i: number): ShorthandValue<BoxProps> {
        if (this.props.options.length > 1) {
            return {
                content: <TrashCanIcon className="choice-trash-can" outline={true} aria-hidden="false"
                    title={this.props.options[i].deleteChoiceLabel}
                    onClick={() => {
                        if (this.currentFocus == this.props.options.length - 1) {
                            setTimeout((() => {
                                this.addButtonRef.focus();
                            }).bind(this), 0);
                        }
                        this.props.onDeleteChoice(i);
                    }}
                />
                
            };
        }
        return null;
    }

    render() {
        let items: JSX.Element[] = [];
        let maxOptions: number = (this.props.limit && this.props.limit > 0) ? this.props.limit : Number.MAX_VALUE;
        let focusOnErrorSet: boolean = false;
        let className: string = "item-content";

        // if any input is blank while submitting action then there will be entry in optionError
        for (let i = 0; i < (maxOptions > this.props.options.length ? this.props.options.length : maxOptions); i++) {
            let errorString = this.props.optionsError && this.props.optionsError.length > i ? this.props.optionsError[i] : "";
            if (errorString.length > 0 && this.props.focusOnError && !focusOnErrorSet) {
                this.currentFocus = i;
                focusOnErrorSet = true;
            }
            let choicePrefix; 
            if (this.props.options[i].checked) {
                choicePrefix = <Status state="success" icon={<AcceptIcon />} title="correct answer" />;
            }
            items.push(
                <div key={"option" + i} className="checklist-item-container">
                    <div className="checklist-item">
                        {this.props.multiselect && <Checkbox disabled={!this.props.quizMode}
                            className="checklist-checkbox"
                            checked={this.props.options[i].checked}
                            onChange={(e, props) => {
                                this.props.onItemChecked(i, props.checked);
                            }}
                        />}
                        {!this.props.multiselect &&
                            <Input
                            onChange={(e, props) => {
                                this.props.onItemChecked(i, e.target.checked);
                            }}
                            checked={this.props.options[i].checked}                            
                            disabled={!this.props.quizMode}
                            className="checklist-checkbox"
                            name="radioChoice"
                            type="radio" />
                        }
                        <div className="choice-item">
                            <InputBox
                                ref={(inputBox) => {
                                    if (inputBox && i == this.currentFocus) {
                                        inputBox.focus();
                                    }
                                }}
                                prefixJSX={choicePrefix}
                                fluid
                                input={{ className }}
                                maxLength={this.props.maxLength}
                                icon={this.getDeleteIconProps(i)}
                                showError={errorString.length > 0}
                                errorText={errorString}
                                key={"option" + i}
                                value={this.props.options[i].value}
                                placeholder={this.props.options[i].choicePlaceholder}
                                onKeyDown={(e) => {
                                    if (!e.repeat && (e.keyCode || e.which) == ChoiceContainer.CARRIAGE_RETURN_ASCII_VALUE
                                        && this.props.options.length < maxOptions) {
                                        if (i == this.props.options.length - 1) {
                                            this.props.onAddChoice();
                                            this.currentFocus = this.props.options.length;
                                        } else {
                                            this.currentFocus += 1;
                                            this.forceUpdate();
                                        }
                                    }
                                }}
                                onFocus={(e) => {
                                    this.currentFocus = i;
                                }}
                                onChange={(e) => {
                                    this.props.onUpdateChoice(i, (e.target as HTMLInputElement).value);
                                }}
                            />
                        </div>
                    </div>
                </div>
            );

            


        }
        return (
            <div
                className="choice-container"
                onBlur={(e) => {
                    this.currentFocus = -1;
                }}>
                {items}
                {this.props.options.length < maxOptions &&
                    <div
                        ref={(e) => {
                            this.addButtonRef = e;
                        }}
                        className={"add-options"}
                        
                        onClick={(e) => {
                            this.props.onAddChoice();
                            this.currentFocus = this.props.options.length;
                        }}
                    >
                        <AddIcon className="plus-icon" outline size="medium" color="brand" />
                        <Text size="medium" content={this.localize("PollAddChoice")} color="brand" />
                    </div>
                }
            </div>
        );
    }
}
export default withTranslation()(ChoiceContainer);