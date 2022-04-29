// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import * as AdaptiveCards from "adaptivecards";
import MarkdownIt from "markdown-it";
import { TFunction } from "i18next";

AdaptiveCards.AdaptiveCard.onProcessMarkdown = function (text, result) {
    //console.log("onProcessMarkdown occured");
    result.outputHtml = new MarkdownIt().render(text);
    result.didProcess = true;
}

AdaptiveCards.AdaptiveCard.onExecuteAction = function (action) { alert('Voted'); };
AdaptiveCards.AdaptiveCard.onInputValueChanged = function (input: AdaptiveCards.Input) {
    console.log(input);
    console.log(input.value);
};

export const getInitAdaptiveCard = (t: TFunction) => {
    const titleTextAsString = t("TitleText");
    return (
        {
            "type": "AdaptiveCard",
            "body": [
                {
                    "type": "TextBlock",
                    "weight": "Bolder",
                    "text": titleTextAsString,
                    "size": "ExtraLarge",
                    "wrap": true
                },
                {
                    "type": "Image",
                    "spacing": "Default",
                    "url": "",
                    "size": "Stretch",
                    "width": "400px",
                    "altText": ""
                },
                {
                    "type": "TextBlock",
                    "text": "",
                    "wrap": true
                },
                {
                    "type": "TextBlock",
                    "wrap": true,
                    "size": "Small",
                    "weight": "Lighter",
                    "text": ""
                },
                {
                    "id": "Choices",
                    "type": "Container",                    
                    "items": [
                        {
                            "type": "Input.ChoiceSet",
                            "id": "PollChoices",
                            "style": "expanded",
                            "isMultiSelect": false,
                            "wrap": "true",
                            "choices": []
                        }
                    ]
                }                
            ],
            actions: [
                {
                    "type": "Action.OpenUrl",
                    "title": "Vote Poll",
                    "url": "https://microsoft.com"
                }
            ],
            "$schema": "https://adaptivecards.io/schemas/adaptive-card.json",
            "version": "1.0"
        }
    );
}

export const getCardTitle = (card: any) => {
    return card.body[0].text;
}

export const setCardTitle = (card: any, title: string) => {
    card.body[0].text = title;
}

export const getCardImageLink = (card: any) => {
    return card.body[1].url;
}

export const setCardImageLink = (card: any, imageLink?: string) => {
    card.body[1].url = imageLink;
}

export const getCardSummary = (card: any) => {
    return card.body[2].text;
}

export const setCardSummary = (card: any, summary?: string) => {
    card.body[2].text = summary;
}

export const getCardAuthor = (card: any) => {
    return card.body[3].text;
}

export const setCardAuthor = (card: any, author?: string) => {
    card.body[3].text = author;
}

export const getCardBtnTitle = (card: any) => {
    return card.actions[0].title;
}

export const getCardBtnLink = (card: any) => {
    return card.actions[0].url;
}

export const setCardBtn = (card: any, buttonTitle?: string, buttonLink?: string) => {
    if (buttonTitle && buttonLink) {
        card.actions = [
            {
                "type": "Action.OpenUrl",
                "title": buttonTitle,
                "url": buttonLink
            }
        ];
    } else {
        delete card.actions;
    }
}

export const setCardHidePoll = (card: any) => {
    card.body[4].isVisible = false;
    delete card.actions;
}

export const setCardPollOptions = (card: any, isMultiselect: boolean, values: string[]) => {
    console.log('setCardPollOptions');
    console.log(values);

    if (values) {
        let choiceOptions: any[] = [];
        let i = 0;
        values.forEach((option) => {
            const choiceOption = {
                title: option,
                value: i.toString(),
            };
            choiceOptions.push(choiceOption);
            if (i === 0) {
                console.log('option: ' + option + ' value: ' + i);
            }
            i++;
        });
        card.body[4].items[0].choices = choiceOptions;
        card.body[4].items[0].isMultiSelect = isMultiselect;
        console.log('card.body[4].items[0].choices');
        console.log(card);
        console.log('end setCardPollOptions');
    } else {
        delete card.body[4].items[0].choices;
    }
}

export const setCardPollQuizSelectedValue = (card: any, value: string) => {    
    card.body[4].items[0].value = value;
    console.log(card);
}

export const getCardPollQuizSelectedValue = (card: any) => {
    console.log(card.body[4].items[0]);
    return card.body[4].items[0].value;
}

//export const getQuizAnswers = (card: any) => {
//    let adaptiveCard = new AdaptiveCards.AdaptiveCard();
//    adaptiveCard.parse(card);
//    console.log(adaptiveCard);
//    adaptiveCard.onExecuteAction = function (action) { alert('Voted'); };
//    let json = adaptiveCard.toJSON();
//    console.log("json");
//    console.log(json);

    

//    let choices = card.body[4].items[0].choices;
//    let selectedChoices: any[] = [];
//    console.log(choices);
//    choices.forEach((c: any) => {
//        console.log(c);
//        if (c.isSelected) {
//           selectedChoices.push(c.value);
//        }
//    })
//    let answers = JSON.stringify(selectedChoices);
//    console.log(answers);
//    return answers;
//}
