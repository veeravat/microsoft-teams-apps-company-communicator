// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import * as React from 'react';
import { withTranslation, WithTranslation } from "react-i18next";
import './statusTaskModule.scss';
import { getSentNotification, exportNotification, getReactionsCount, getPollResult, getCorrectQuizesCount, getVotesCount } from '../../apis/messageListApi';
import { RouteComponentProps } from 'react-router-dom';
import * as AdaptiveCards from "adaptivecards";
import { ProgressIndicator, Stack, TooltipHost } from '@fluentui/react';
import { Loader, List, Image, Text, Button, DownloadIcon, AcceptIcon, Flex, Avatar, Status, CloseIcon, Divider } from '@fluentui/react-northstar';
import * as microsoftTeams from "@microsoft/teams-js";
import {
    getInitAdaptiveCard, setCardTitle, setCardImageLink, setCardSummary,
    setCardAuthor, setCardBtn, setCardPollOptions, setCardHidePoll
} from '../AdaptiveCard/adaptiveCardPoll';
import { ImageUtil } from '../../utility/imageutility';
import { formatDate, formatDuration, formatNumber } from '../../i18n';
import { TFunction } from "i18next";

export interface IListItem {
    header: string,
    media: JSX.Element,
}

export interface IMessage {
    id: string;
    title: string;
    acknowledgements?: string;
    reactions?: string;
    responses?: string;
    succeeded?: string;
    viewsCount?: string;
    clicksCount?: string;
    acknowledgementsCount?: string;
    reactionsCount?: string;
    failed?: string;
    unknown?: string;
    canceled?: string;
    sentDate?: string;
    sentBy?: string;
    imageLink?: string;
    summary?: string;
    author?: string;
    buttonLink?: string;
    buttonTitle?: string;
    teamNames?: string[];
    rosterNames?: string[];
    groupNames?: string[];
    allUsers?: boolean;
    sendingStartedDate?: string;
    sendingDuration?: string;
    errorMessage?: string;
    warningMessage?: string;
    canDownload?: boolean;
    sendingCompleted?: boolean;
    messageType?: string;
    pollOptions?: string,
    isPollMultipleChoice?: boolean;
    isPollQuizMode?: boolean;
    pollQuizAnswers?: string;
}

export interface IStatusState {
    message: IMessage;
    loader: boolean;
    page: string;
    teamId?: string;
    reactionsCount: number;
    correctQuizesCount: number;
    votesCount: number;
    pollResultsChartData?: any;
    isPollMultipleChoice: boolean;
    sentByUserPhoto: any;
}

interface StatusTaskModuleProps extends RouteComponentProps, WithTranslation { }

class StatusTaskModule extends React.Component<StatusTaskModuleProps, IStatusState> {
    readonly localize: TFunction;
    private initMessage = {
        id: "",
        title: ""
    };

    private card: any;

    constructor(props: StatusTaskModuleProps) {
        super(props);

        this.localize = this.props.t;

        this.card = getInitAdaptiveCard(this.props.t);

        this.state = {
            message: this.initMessage,
            loader: true,
            page: "ViewStatus",
            teamId: '',
            reactionsCount: 0,
            correctQuizesCount: 0,
            votesCount: 0,
            isPollMultipleChoice: false,
            sentByUserPhoto: '',
        };
    }

    public componentDidMount() {
        let params = this.props.match.params;
        microsoftTeams.initialize();
        microsoftTeams.getContext((context) => {
            this.setState({
                teamId: context.teamId,
            });
        });

        if ('id' in params) {
            let id = params['id'];
            this.getItem(id).then(() => {
                this.setState({
                    loader: false
                }, () => {
                    setCardTitle(this.card, this.state.message.title);
                    setCardImageLink(this.card, this.state.message.imageLink);
                    setCardSummary(this.card, this.state.message.summary);
                    setCardAuthor(this.card, this.state.message.author);

                    if (this.state.message.pollOptions) {
                        setCardBtn(this.card, this.localize("PollSubmitVote"), "https://adaptivecards.io");
                        const options: string[] = JSON.parse(this.state.message.pollOptions);
                        setCardPollOptions(this.card, this.state.isPollMultipleChoice, options);

                        let choiceOptions = new Map();
                        let i = 0;
                        options.forEach((option) => {
                            const choiceOption = {
                                title: option,
                                count: 0,
                                answer: false,
                            };
                            choiceOptions.set(i.toString(), choiceOption);
                            i++;
                        });

                        Promise.all([this.getPollResult(id), this.getVotesCount(id), this.getCorrectQuizesCount(id)]).then((responses) => {
                            console.log('choiceOptions init');
                            console.log(choiceOptions);
                            if (this.state.message.isPollQuizMode && this.state.message.pollQuizAnswers) {
                                const answers: string[] = JSON.parse(this.state.message.pollQuizAnswers);
                                console.log('answers ');
                                console.log(answers);
                                for (var jj = 0; jj < answers.length; jj++) {
                                    let optionNum = answers[jj];
                                    console.log("optionNum: " + optionNum);
                                    let current = choiceOptions.get(optionNum.toString());
                                    console.log(current);
                                    current.answer = true;
                                    choiceOptions.set(optionNum.toString(), current);
                                }
                            }
                            console.log('choiceOptions after quiz answers');
                            console.log(choiceOptions);

                            let rows = responses[0].data.tables[0].rows;
                            if (rows) {
                                for (var j = 0; j < rows.length; j++) {
                                    let optionNum = rows[j][0];
                                    let optionCount = rows[j][1];
                                    console.log("optionNum: " + optionNum + " optionCount: " + optionCount);
                                    let current = choiceOptions.get(optionNum);
                                    console.log(current);
                                    current.count = optionCount;
                                    choiceOptions.set(optionNum, current);
                                }
                            }
                            console.log('choiceOptions add counts');
                            console.log(choiceOptions);
                          
                            this.setState({
                                pollResultsChartData:
                                    { choiceOptions: choiceOptions }
                            });
                            console.log(this.state.pollResultsChartData);
                            
                        });
                    }
                    else {
                        setCardHidePoll(this.card);
                    }


                    if (this.state.message.buttonTitle !== "" && this.state.message.buttonLink !== "") {
                        setCardBtn(this.card, this.state.message.buttonTitle, this.state.message.buttonLink);
                    }

                    let adaptiveCard = new AdaptiveCards.AdaptiveCard();
                    adaptiveCard.parse(this.card);
                    let renderedCard = adaptiveCard.render();

                    if (renderedCard) {
                        const inputs = renderedCard.getElementsByTagName('input');
                        Array.from(inputs).forEach((inputElement: Element): void => {
                            (inputElement as HTMLInputElement).disabled = true;
                        });

                        const container = document.getElementsByClassName('adaptiveCardContainer')[0].firstChild;
                        if (container != null) {
                            container.replaceWith(renderedCard);
                        } else {
                            document.getElementsByClassName('adaptiveCardContainer')[0].appendChild(renderedCard);
                        }
                    }                                        
                });
            });
            this.getReactionsCount(id);
        }
    }

    private getReactionsCount = async (id: number) => {
        try {
            const response = await getReactionsCount(id);
            response.data = formatNumber(response.data);
            this.setState({
                reactionsCount: response.data
            });
        } catch (error) {
            return error;
        }
    }

    private getCorrectQuizesCount = async (id: number) => {
        try {
            if (this.state.message.isPollQuizMode) {
                const response = await getCorrectQuizesCount(id);
                const count = response.data;
                this.setState({
                    correctQuizesCount: count
                });
            }            
        } catch (error) {
            return error;
        }
    }

    private getVotesCount = async (id: number) => {
        try {
            const response = await getVotesCount(id);
            const count = response.data;
            this.setState({
                votesCount: count
            });
        } catch (error) {
            return error;
        }
    }

    

    private getPollResult = async (id: number) => {
        try {
            const response = await getPollResult(id);
            console.log(response.data);
            return response;
            
        } catch (error) {
            return error;
        }
    }

    private getItem = async (id: number) => {
        try {
            const response = await getSentNotification(id);
            response.data.sendingDuration = formatDuration(response.data.sendingStartedDate, response.data.sentDate);
            response.data.sendingStartedDate = formatDate(response.data.sendingStartedDate);
            response.data.sentDate = formatDate(response.data.sentDate);
            response.data.succeeded = formatNumber(response.data.succeeded);
            response.data.viewsCount = formatNumber(response.data.viewsCount);
            response.data.clicksCount = formatNumber(response.data.clicksCount);
            response.data.acknowledgementsCount = formatNumber(response.data.acknowledgementsCount);
            response.data.failed = formatNumber(response.data.failed);
            response.data.unknown = response.data.unknown && formatNumber(response.data.unknown);
            response.data.canceled = response.data.canceled && formatNumber(response.data.canceled);            
            this.setState({
                message: response.data
            });
        } catch (error) {
            return error;
        }
    }

    private renderRollResults = () => {
        let results = this.state.pollResultsChartData;

        const correctAnswerStatus = <Status state="success" icon={<AcceptIcon />} title={this.localize("PollQuizCorrectAnswer")} className="choice-item-circle" />;
        const wrongAnswerStatus = <Status state="error" icon={<CloseIcon />} className="choice-item-circle" />;

        let items: JSX.Element[] = [];
        results.choiceOptions.forEach((resultItem) => {
            items.push(    
                <Stack horizontal={true} horizontalAlign="space-between">
                    <Stack.Item className="choice-item-circle">
                        {resultItem.answer && this.state.message.isPollQuizMode && correctAnswerStatus}
                        {!resultItem.answer && this.state.message.isPollQuizMode && wrongAnswerStatus}
                    </Stack.Item>
                     

                    <Stack.Item grow={1}>
                        <ProgressIndicator label={resultItem.title} barHeight={4} percentComplete={this.state.votesCount === 0 ? 0 : resultItem.count / this.state.votesCount } />
                    </Stack.Item>
                    <Stack.Item align="end">
                        <Text> {this.state.votesCount === 0 ? 0 : ((resultItem.count / this.state.votesCount) * 100).toFixed()}% ({resultItem.count})</Text>
                    </Stack.Item>
                </Stack>
            );
        });
        return (
            <>
                {this.state.message.succeeded && this.state.message.succeeded !== '0' &&
                    <ProgressIndicator
                        label={this.localize("PollParticipation", { "percent": (this.state.votesCount / parseInt(this.state.message.succeeded) * 100).toFixed() })}
                        description={
                            (this.state.votesCount === 1) ?
                                this.localize("PollParticipationIndicatorSingular", { "votes": this.state.votesCount, "succeeded": this.state.message.succeeded }) :
                                this.localize("PollParticipationIndicatorPlural", { "votes": this.state.votesCount, "succeeded": this.state.message.succeeded })
                        }
                        barHeight={6}
                        percentComplete={this.state.votesCount / parseInt(this.state.message.succeeded)} />
                }
                <Divider />
                {
                    this.state.message.isPollQuizMode && ((this.state.correctQuizesCount === 0) ?
                        this.localize("PollQuizNoCorrectAnswers") :
                        this.localize("PollQuizResults", {
                            "percent": ((this.state.correctQuizesCount / this.state.votesCount) * 100).toFixed(),
                            "correct": this.state.correctQuizesCount,
                            "votes": this.state.votesCount,
                        }))
                }
                <Divider />
                {items}
            </>            
            );
    }

    public render(): JSX.Element {
        if (this.state.loader) {
            return (
                <div className="Loader">
                    <Loader />
                </div>
            );
        } else {
            if (this.state.page === "ViewStatus") {
                return (
                    <div className="taskModule">
                        <Flex column className="formContainer" vAlign="stretch" gap="gap.small">
                            <Flex className="scrollableContent">
                                <Flex.Item size="size.half" className="formContentContainer">
                                    <Flex column>
                                        <div className="contentField">
                                            <h3>{this.localize("TitleText")}</h3>
                                            <span>{this.state.message.title}</span>
                                        </div>
                                        {this.state.message.messageType === 'Poll' && this.state.pollResultsChartData &&
                                            <div className="contentField">
                                                <h3>{this.localize("PollResultsTitle")}</h3>
                                                {this.renderRollResults()}
                                            </div>
                                        }
                                        <div className="contentField">
                                            <h3>{this.localize("SentBy")}</h3>
                                            <span>{this.state.message.sentBy}</span>
                                            <Avatar name={this.state.message.sentBy} size="smallest"
                                                image={this.state.sentByUserPhoto} />
                                        </div>
                                        <div className="contentField">
                                            <h3>{this.localize("SendingStarted")}</h3>
                                            <span>{this.state.message.sendingStartedDate}</span>
                                        </div>
                                        <div className="contentField">
                                            <h3>{this.localize("Completed")}</h3>
                                            <span>{this.state.message.sentDate}</span>
                                        </div>
                                        <div className="contentField">
                                            <h3>{this.localize("Duration")}</h3>
                                            <span>{this.state.message.sendingDuration}</span>
                                        </div>
                                        <div className="contentField">
                                            <h3>{this.localize("Results")}</h3>
                                            <label>{this.localize("Success", { "SuccessCount": this.state.message.succeeded })}</label>
                                            <br />
                                            <label>{this.localize("Views", { "ViewsCount": this.state.message.viewsCount })}</label>
                                            <br />
                                            { this.state.message.messageType !== 'Poll' && 
                                                <>
                                                <label>{this.localize("Clicks", { "ClicksCount": this.state.message.clicksCount })}</label>
                                                <br />
                                                <label>{this.localize("Acknowledgements", { "AcknowledgementsCount": this.state.message.acknowledgementsCount })}</label>
                                                <br />
                                                </>
                                            }
                                            
                                            <label>{this.localize("Reactions", { "ReactionsCount": this.state.reactionsCount })}</label>

                                            <br />
                                            <label>{this.localize("Failure", { "FailureCount": this.state.message.failed })}</label>
                                            {this.state.message.canceled &&
                                                <>
                                                    <br />
                                                    <label>{this.localize("Canceled", { "CanceledCount": this.state.message.canceled })}</label>
                                                </>
                                            }
                                            {this.state.message.unknown &&
                                                <>
                                                    <br />
                                                    <label>{this.localize("Unknown", { "UnknownCount": this.state.message.unknown })}</label>
                                                </>
                                            }
                                        </div>                                        
                                        
                                        <div className="contentField">
                                            {this.renderAudienceSelection()}
                                        </div>
                                        <div className="contentField">
                                            {this.renderErrorMessage()}
                                        </div>
                                        <div className="contentField">
                                            {this.renderWarningMessage()}
                                        </div>
                                    </Flex>
                                </Flex.Item>
                                <Flex.Item size="size.half">
                                    <div className="adaptiveCardContainer">
                                    </div>
                                </Flex.Item>
                            </Flex>
                            <Flex className="footerContainer" vAlign="end" hAlign="end">
                                <div className={this.state.message.canDownload ? "" : "disabled"}>
                                    <Flex className="buttonContainer" gap="gap.small">
                                        <Flex.Item push>
                                            <Loader id="sendingLoader" className="hiddenLoader sendingLoader" size="smallest" label={this.localize("ExportLabel")} labelPosition="end" />
                                        </Flex.Item>
                                        <Flex.Item>
                                            <TooltipHost content={!this.state.message.sendingCompleted ? "" : (this.state.message.canDownload ? "" : this.localize("ExportButtonProgressText"))} calloutProps={{ gapSpace: 0 }}>
                                                <Button icon={<DownloadIcon size="medium" />} disabled={!this.state.message.canDownload || !this.state.message.sendingCompleted} content={this.localize("ExportButtonText")} id="exportBtn" onClick={this.onExport} primary />
                                            </TooltipHost>
                                        </Flex.Item>
                                    </Flex>
                                </div>
                            </Flex>
                        </Flex>
                    </div>
                );
            }
            else if (this.state.page === "SuccessPage") {
                return (
                    <div className="taskModule">
                        <Flex column className="formContainer" vAlign="stretch" gap="gap.small">
                            <div className="displayMessageField">
                                <br />
                                <br />
                                <div><span><AcceptIcon className="iconStyle" xSpacing="before" size="largest" outline /></span>
                                    <h1>{this.localize("ExportQueueTitle")}</h1></div>
                                <span>{this.localize("ExportQueueSuccessMessage1")}</span>
                                <br />
                                <br />
                                <span>{this.localize("ExportQueueSuccessMessage2")}</span>
                                <br />
                                <span>{this.localize("ExportQueueSuccessMessage3")}</span>
                            </div>
                            <Flex className="footerContainer" vAlign="end" hAlign="end" gap="gap.small">
                                <Flex className="buttonContainer">
                                    <Button content={this.localize("CloseText")} id="closeBtn" onClick={this.onClose} primary />
                                </Flex>
                            </Flex>
                        </Flex>
                    </div>
                );
            }
            else {
                return (
                    <div className="taskModule">
                        <Flex column className="formContainer" vAlign="stretch" gap="gap.small">
                            <div className="displayMessageField">
                                <br />
                                <br />
                                <div><span></span>
                                    <h1 className="light">{this.localize("ExportErrorTitle")}</h1></div>
                                <span>{this.localize("ExportErrorMessage")}</span>
                            </div>
                            <Flex className="footerContainer" vAlign="end" hAlign="end" gap="gap.small">
                                <Flex className="buttonContainer">
                                    <Button content={this.localize("CloseText")} id="closeBtn" onClick={this.onClose} primary />
                                </Flex>
                            </Flex>
                        </Flex>
                    </div>
                );
            }
        }
    }

    private onClose = () => {
        microsoftTeams.tasks.submitTask();
    }

    private onExport = async () => {
        let spanner = document.getElementsByClassName("sendingLoader");
        spanner[0].classList.remove("hiddenLoader");
        let payload = {
            id: this.state.message.id,
            teamId: this.state.teamId
        };
        await exportNotification(payload).then(() => {
            this.setState({ page: "SuccessPage" });
        }).catch(() => {
            this.setState({ page: "ErrorPage" });
        });
    }

    private getItemList = (items: string[]) => {
        let resultedTeams: IListItem[] = [];
        if (items) {
            resultedTeams = items.map((element) => {
                const resultedTeam: IListItem = {
                    header: element,
                    media: <Image src={ImageUtil.makeInitialImage(element)} avatar />
                }
                return resultedTeam;
            });
        }
        return resultedTeams;
    }
    private renderAudienceSelection = () => {
        if (this.state.message.teamNames && this.state.message.teamNames.length > 0) {
            return (
                <div>
                    <h3>{this.localize("SentToGeneralChannel")}</h3>
                    <List items={this.getItemList(this.state.message.teamNames)} />
                </div>);
        } else if (this.state.message.rosterNames && this.state.message.rosterNames.length > 0) {
            return (
                <div>
                    <h3>{this.localize("SentToRosters")}</h3>
                    <List items={this.getItemList(this.state.message.rosterNames)} />
                </div>);
        } else if (this.state.message.groupNames && this.state.message.groupNames.length > 0) {
            return (
                <div>
                    <h3>{this.localize("SentToGroups1")}</h3>
                    <span>{this.localize("SentToGroups2")}</span>
                    <List items={this.getItemList(this.state.message.groupNames)} />
                </div>);
        } else if (this.state.message.allUsers) {
            return (
                <div>
                    <h3>{this.localize("SendToAllUsers")}</h3>
                </div>);
        } else {
            return (<div></div>);
        }
    }
    private renderErrorMessage = () => {
        if (this.state.message.errorMessage) {
            return (
                <div>
                    <h3>{this.localize("Errors")}</h3>
                    <span>{this.state.message.errorMessage}</span>
                </div>
            );
        } else {
            return (<div></div>);
        }
    }

    private renderWarningMessage = () => {
        if (this.state.message.warningMessage) {
            return (
                <div>
                    <h3>{this.localize("Warnings")}</h3>
                    <span>{this.state.message.warningMessage}</span>
                </div>
            );
        } else {
            return (<div></div>);
        }
    }
}

const StatusTaskModuleWithTranslation = withTranslation()(StatusTaskModule);
export default StatusTaskModuleWithTranslation;