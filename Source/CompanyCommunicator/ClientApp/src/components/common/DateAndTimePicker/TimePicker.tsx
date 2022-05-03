// <copyright file="timepicker.tsx" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

import * as React from 'react';
import { Input, Popup, Flex, Dropdown, Text, ShiftActivityIcon } from '@fluentui/react-northstar';
import { useTranslation } from 'react-i18next';
import "./timepicker.scss";

export enum LanguageDirection {
    /** Indicates that the language direction is right-to-left*/
    Rtl = "rtl",

    /** Indicates that the language direction is left-to-right*/
    Ltr = "ltr",

    /** Indicates that the language direction is auto*/
    Auto = "auto",
}

export interface ITimePickerProps {
    onPickerClose: (hours: number, minutes: number) => void,
    hours?: number,
    minutes?: number,
    minHours?: number,
    minMinutes?: number,
    isDisabled: boolean,
    dir: LanguageDirection
}

const TimePicker: React.FC<ITimePickerProps> = (props) => {
    const localize = useTranslation().t;
    const [open, setOpen] = React.useState(false);
    const [text, setText] = React.useState(props.hours!.toString().padStart(2, '0') + ":" + props.minutes!.toString().padStart(2, '0'));
    const [hours, setHour] = React.useState(props.hours!.toString().padStart(2, '0'));
    const [minutes, setMinute] = React.useState(props.minutes!.toString().padStart(2, '0'));
    const [minHours, setMinHour] = React.useState(props.minHours!);
    const [minMinutes, setMinMinute] = React.useState(props.minMinutes!);

    const hoursItems: Array<string> = [];
    const minutesItems: Array<string> = [];

    //var date: Date = new Date(Date.UTC(2012, 11, 12, 3, 0, 0));
    //// toLocaleTimeString() without arguments depends on the implementation,
    //// the default locale, and the default time zone
    //console.log(date.toLocaleTimeString());
    //// → "7:00:00 PM" if run in en-US locale with time zone America/Los_Angeles

    React.useEffect(() => {
        setHour(props.hours!.toString().padStart(2, '0'));
        setText(props.hours!.toString().padStart(2, '0') + ":" + props.minutes!.toString().padStart(2, '0'));
    }, [props.hours]);
    React.useEffect(() => {
        setMinute(props.minutes!.toString().padStart(2, '0'));
        setText(props.hours!.toString().padStart(2, '0') + ":" + props.minutes!.toString().padStart(2, '0'));
    }, [props.minutes]);
    React.useEffect(() => {
        setMinHour(props.minHours!);
    }, [props.minHours]);
    React.useEffect(() => {
        setMinMinute(props.minMinutes!);
    }, [props.minMinutes]);


    //const getTimePickerItem = (hours: number, minutes: number, locale: string = navigator.language): void => {
    //    let timePickerItem: any = {
    //        hours: hours,
    //        minutes: minutes,
    //        value: hours + ":" + minutes,
    //        asString: null
    //    };
    //    let date = new Date();
    //    date.setHours(hours);
    //    date.setMinutes(minutes);
    //    timePickerItem.asString = date.toLocaleTimeString(locale,
    //        { hour: "2-digit", minute: "2-digit", hour12: true });
    //    //return timePickerItem;
    //    console.log(timePickerItem.asString);
    //}


    for (var i = minHours ? minHours : 0; i < 24; i++) {
        hoursItems.push(i.toString().padStart(2, '0'));
        //getTimePickerItem(i, 0);
    }
    for (var i = 1; i < 12; i++) {
        minutesItems.push((i * 5).toString().padStart(2, '0'));
    }




    const onHourChange = {


        onAdd: (item: any) => {
            if (item) {
                setHour(item);
            }
            return "";
        }
    }

    const onMinuteChange = {
        onAdd: (item: any) => {
            if (item) {
                setMinute(item);
            }
            return "";
        }
    }

    const onOpenChange = (e: any, { open }: any) => {
        setOpen(open)
        if (!open) {
            setText(hours + ":" + minutes);
            props.onPickerClose(parseInt(hours), parseInt(minutes));
        }
    }

    const popupContent = (
        <div className="timepicker-popup-style">
            <Flex gap="gap.small">
                <Flex.Item size="size.half">
                    <Text size={props.dir === LanguageDirection.Rtl ? "medium" : "small"} content={localize("hourPlaceholder")} />
                </Flex.Item>
                <Flex.Item size="size.half" className={props.dir === LanguageDirection.Rtl ? "rtl-right-margin-large" : ""}>
                    <Text size={props.dir === LanguageDirection.Rtl ? "medium" : "small"} content={localize("minutePlaceholder")} />
                </Flex.Item>
            </Flex>
            <Flex gap="gap.small" styles={{ marginTop: "0.5rem" }}>
                <Flex.Item>
                    <Dropdown
                        className="timepicker-dropdown"
                        items={hoursItems}
                        value={hours}
                        placeholder={localize("hourPlaceholder")}
                        getA11ySelectionMessage={onHourChange}
                    />
                </Flex.Item>
                <Flex.Item>
                    <Dropdown
                        className={props.dir === LanguageDirection.Rtl ? "timepicker-dropdown rtl-right-margin-small" : "timepicker-dropdown"}
                        value={minutes}
                        items={minutesItems}
                        placeholder={localize("minutePlaceholder")}
                        getA11ySelectionMessage={onMinuteChange}
                    />
                </Flex.Item>
            </Flex>
        </div>
    );

    return (
        <Popup className="timepicker-popup-style"
            open={open}
            trapFocus
            onOpenChange={onOpenChange}
            trigger={<Input style={{ "width": "100px" }}
                className={props.dir === LanguageDirection.Rtl ? "rtl-left-margin-small" : ""}
                disabled={props.isDisabled} fluid value={text} icon={<ShiftActivityIcon />}
                iconPosition={props.dir === LanguageDirection.Rtl ? "start" : "end"}
            />}
            content={popupContent}
        />
    );
}

export default TimePicker;

TimePicker.defaultProps = {
    hours: 0,
    minutes: 0
};