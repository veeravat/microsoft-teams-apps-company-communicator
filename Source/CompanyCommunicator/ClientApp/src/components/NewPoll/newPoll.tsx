// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import * as React from 'react';
import { RouteComponentProps } from 'react-router-dom';
import { withTranslation, WithTranslation } from "react-i18next";
import { initializeIcons } from 'office-ui-fabric-react/lib/Icons';
import * as AdaptiveCards from "adaptivecards";
import { Button, Loader, Dropdown, Text, Flex, Input, TextArea, RadioGroup, Checkbox, Datepicker, CircleIcon } from '@fluentui/react-northstar'
import * as microsoftTeams from "@microsoft/teams-js";
import { SimpleMarkdownEditor } from 'react-simple-markdown-editor';

import './newPoll.scss';
import './teamPoll.scss';
import { getDraftNotification, getTeams, createDraftNotification, updateDraftNotification, searchGroups, getGroups, verifyGroupAccess } from '../../apis/messageListApi';
import {
    getInitAdaptiveCard, setCardTitle, setCardImageLink, setCardSummary,
    setCardAuthor, setCardBtn, setCardPollOptions, getQuizAnswers, setCardPollQuizSelectedValue
} from '../AdaptiveCard/adaptiveCardPoll';
import { getBaseUrl } from '../../configVariables';
import { ImageUtil } from '../../utility/imageutility';
import { TFunction } from "i18next";
import TimePicker, { LanguageDirection } from '../common/TimePicker';
import LocalizedDatePicker from '../common/LocalizedDatePicker';
import ChoiceContainer, { IChoiceContainerOption } from '../common/ChoiceContainer/ChoiceContainer';

type dropdownItem = {
    key: string,
    header: string,
    content: string,
    image: string,
    team: {
        id: string,
    },
}

export interface IDraftMessage {
    id?: string,
    title: string,
    imageLink?: string,
    summary?: string,
    author: string,
    buttonTitle?: string,
    buttonLink?: string,
    teams: any[],
    rosters: any[],
    groups: any[],
    allUsers: boolean,

    ack?: boolean,
    delayDelivery?: boolean,
    inlineTranslation?: boolean,
    scheduledDateTime?: Date,
    fullWidth?: boolean,
    notifyUser?: boolean,

    pollOptions: string,
    messageType: string,
    isAnonymousVoting?: boolean,
    isPollMultipleChoice: boolean;
    isPollQuizMode: boolean;
    pollQuizAnswers?: string;
}

export interface formState {
    title: string,
    summary?: string,
    btnLink?: string,
    imageLink?: string,
    btnTitle?: string,
    author: string,
    card?: any,
    page: string,
    teamsOptionSelected: boolean,
    rostersOptionSelected: boolean,
    allUsersOptionSelected: boolean,
    groupsOptionSelected: boolean,
    teams?: any[],
    groups?: any[],
    exists?: boolean,
    messageId: string,
    loader: boolean,
    groupAccess: boolean,
    loading: boolean,
    noResultMessage: string,
    unstablePinned?: boolean,
    selectedTeamsNum: number,
    selectedRostersNum: number,
    selectedGroupsNum: number,
    selectedRadioBtn: string,
    selectedTeams: dropdownItem[],
    selectedRosters: dropdownItem[],
    selectedGroups: dropdownItem[],
    pollOptions: string[],
    messageType: string,
    errorImageUrlMessage: string,
    errorButtonUrlMessage: string,
    selectedRequestReadReceipt?: boolean,
    selectedDelayDelivery?: boolean,
    selectedInlineTranslation?: boolean,
    selectedScheduledDateTime?: Date,
    fullWidth?: boolean,
    notifyUser?: boolean,

    isPollMultipleChoice: boolean;
    isPollQuizMode: boolean;
    pollQuizAnswers?: number[];
    choiceOptions: IChoiceContainerOption[];
}

export interface INewPollProps extends RouteComponentProps, WithTranslation {
    getDraftMessagesList?: any;
}

class NewPoll extends React.Component<INewPollProps, formState> {
    readonly localize: TFunction;
    private card: any;
    private fileInput: any;

    constructor(props: INewPollProps) {
        super(props);
        initializeIcons();
        this.localize = this.props.t;
        this.card = getInitAdaptiveCard(this.localize);
        this.setDefaultCard(this.card);

        this.state = {
            title: "",
            summary: "",
            author: "",
            btnLink: "",
            imageLink: "",
            btnTitle: "",
            card: this.card,
            page: "CardCreation",
            teamsOptionSelected: true,
            rostersOptionSelected: false,
            allUsersOptionSelected: false,
            groupsOptionSelected: false,
            messageId: "",
            loader: true,
            groupAccess: false,
            loading: false,
            noResultMessage: "",
            unstablePinned: true,
            selectedTeamsNum: 0,
            selectedRostersNum: 0,
            selectedGroupsNum: 0,
            selectedRadioBtn: "teams",
            selectedTeams: [],
            selectedRosters: [],
            selectedGroups: [],
            pollOptions: [],
            selectedRequestReadReceipt: false,
            selectedInlineTranslation: false,
            selectedScheduledDateTime: new Date(),
            selectedDelayDelivery: false, 
            errorImageUrlMessage: "",
            errorButtonUrlMessage: "",

            messageType: 'Poll',
            isPollQuizMode: false,
            isPollMultipleChoice: false,
            pollQuizAnswers: [],
            choiceOptions: [],
        }

        this.fileInput = React.createRef();
        this.handleImageSelection = this.handleImageSelection.bind(this);
    }

    public async componentDidMount() {
        microsoftTeams.initialize();
        //- Handle the Esc key
        document.addEventListener("keydown", this.escFunction, false);
        let params = this.props.match.params;
        this.setGroupAccess();
        this.getTeamList().then(() => {
            if ('id' in params) { // existing message
                let id = params['id'];
                this.getItem(id).then(() => {
                    const selectedTeams = this.makeDropdownItemList(this.state.selectedTeams, this.state.teams);
                    const selectedRosters = this.makeDropdownItemList(this.state.selectedRosters, this.state.teams);
                    this.setState({
                        exists: true,
                        messageId: id,
                        selectedTeams: selectedTeams,
                        selectedRosters: selectedRosters,
                    });

                    setCardPollOptions(this.state.card, this.state.isPollMultipleChoice, this.state.pollOptions);
                    this.updateCard();
                });
                this.getGroupData(id).then(() => {
                    const selectedGroups = this.makeDropdownItems(this.state.groups);
                    this.setState({
                        selectedGroups: selectedGroups
                    })
                });
            } else {
                // new message
                //let options: string[] = [ this.localize("PollChoice", { "choiceNumber": 0 }) ];
                
                this.setState({
                    exists: false,
                    loader: false,
                    //pollOptions: options
                }, () => {
                    //setCardPollOptions(this.state.card, this.state.isPollMultipleChoice, this.state.pollOptions);
                    //this.updateCard();
                    let adaptiveCard = new AdaptiveCards.AdaptiveCard();
                    adaptiveCard.parse(this.state.card);
                    let renderedCard = adaptiveCard.render();
                    document.getElementsByClassName('adaptiveCardContainer')[0].appendChild(renderedCard);
                    this.addChoice();
                });
            }
        });
    }

    private getItem = async (id: number) => {
        try {
            const response = await getDraftNotification(id);
            const draftMessageDetail = response.data;
            let selectedRadioButton = "teams";
            if (draftMessageDetail.rosters.length > 0) {
                selectedRadioButton = "rosters";
            }
            else if (draftMessageDetail.groups.length > 0) {
                selectedRadioButton = "groups";
            }
            else if (draftMessageDetail.allUsers) {
                selectedRadioButton = "allUsers";
            }

            this.setState({
                teamsOptionSelected: draftMessageDetail.teams.length > 0,
                selectedTeamsNum: draftMessageDetail.teams.length,
                rostersOptionSelected: draftMessageDetail.rosters.length > 0,
                selectedRostersNum: draftMessageDetail.rosters.length,
                groupsOptionSelected: draftMessageDetail.groups.length > 0,
                selectedGroupsNum: draftMessageDetail.groups.length,
                selectedRadioBtn: selectedRadioButton,
                selectedTeams: draftMessageDetail.teams,
                selectedRosters: draftMessageDetail.rosters,
                selectedGroups: draftMessageDetail.groups,

                selectedRequestReadReceipt: draftMessageDetail.ack,
                selectedInlineTranslation: draftMessageDetail.inlineTranslation,
                selectedScheduledDateTime: draftMessageDetail.scheduledDateTime !== null ? new Date(draftMessageDetail.scheduledDateTime) : draftMessageDetail.scheduledDateTime,

                selectedDelayDelivery: draftMessageDetail.scheduledDateTime !== null,

                fullWidth: draftMessageDetail.fullWidth,
                notifyUser: draftMessageDetail.notifyUser,

                pollOptions: draftMessageDetail.pollOptions ? JSON.parse(draftMessageDetail.pollOptions) : [],
                isPollMultipleChoice: draftMessageDetail.isPollMultipleChoice,
                isPollQuizMode: draftMessageDetail.isPollQuizMode,
                pollQuizAnswers: draftMessageDetail.pollQuizAnswers ? JSON.parse(draftMessageDetail.pollQuizAnswers) : [],
            });

            setCardTitle(this.card, draftMessageDetail.title);
            setCardImageLink(this.card, draftMessageDetail.imageLink);
            setCardSummary(this.card, draftMessageDetail.summary);
            setCardAuthor(this.card, draftMessageDetail.author);
            setCardPollOptions(this.card, this.state.isPollMultipleChoice, this.state.pollOptions);

            this.setState({
                title: draftMessageDetail.title,
                summary: draftMessageDetail.summary,
                btnLink: draftMessageDetail.buttonLink,
                imageLink: draftMessageDetail.imageLink,
                btnTitle: draftMessageDetail.buttonTitle,
                author: draftMessageDetail.author,
                allUsersOptionSelected: draftMessageDetail.allUsers,

                loader: false
            }, () => {
                this.updateCard();
            });
        } catch (error) {
            return error;
        }
    }

    private makeDropdownItems = (items: any[] | undefined) => {
        const resultedTeams: dropdownItem[] = [];
        if (items) {
            items.forEach((element) => {
                resultedTeams.push({
                    key: element.id,
                    header: element.name,
                    content: element.mail,
                    image: ImageUtil.makeInitialImage(element.name),
                    team: {
                        id: element.id
                    },

                });
            });
        }
        return resultedTeams;
    }

    private makeDropdownItemList = (items: any[], fromItems: any[] | undefined) => {
        const dropdownItemList: dropdownItem[] = [];
        items.forEach(element =>
            dropdownItemList.push(
                typeof element !== "string" ? element : {
                    key: fromItems!.find(x => x.id === element).id,
                    header: fromItems!.find(x => x.id === element).name,
                    image: ImageUtil.makeInitialImage(fromItems!.find(x => x.id === element).name),
                    team: {
                        id: element
                    }
                })
        );
        return dropdownItemList;
    }

    public setDefaultCard = (card: any) => {
        const titleAsString = this.localize("TitleText");
        const summaryAsString = this.localize("Summary");
        const authorAsString = this.localize("Author1");

        setCardTitle(card, titleAsString);
        let imgUrl = getBaseUrl() + "/image/imagePlaceholder.png";
        setCardImageLink(card, imgUrl);
        setCardSummary(card, summaryAsString);
        setCardAuthor(card, authorAsString);
        setCardBtn(card, this.localize("PollSubmitVote"), "https://adaptivecards.io");
        
    }

    private getTeamList = async () => {
        try {
            const response = await getTeams();
            this.setState({
                teams: response.data
            });
        } catch (error) {
            return error;
        }
    }

    private getGroupItems() {
        if (this.state.groups) {
            return this.makeDropdownItems(this.state.groups);
        }
        const dropdownItems: dropdownItem[] = [];
        return dropdownItems;
    }

    private setGroupAccess = async () => {
        await verifyGroupAccess().then(() => {
            this.setState({
                groupAccess: true
            });
        }).catch((error) => {
            const errorStatus = error.response.status;
            if (errorStatus === 403) {
                this.setState({
                    groupAccess: false
                });
            }
            else {
                throw error;
            }
        });
    }

    private getGroupData = async (id: number) => {
        try {
            const response = await getGroups(id);
            this.setState({
                groups: response.data
            });
        }
        catch (error) {
            return error;
        }
    }

    public componentWillUnmount() {
        document.removeEventListener("keydown", this.escFunction, false);
    }

    _handleReaderLoaded = (readerEvt: any) => {
        const binaryString = readerEvt.target.result;
        console.log(binaryString);
    }

    handleImageSelection() {
        const file = this.fileInput.current.files[0];
        const { type: mimeType } = file;
        console.log('file.size: ' + file.size); console.log('mimeType: ' + mimeType);

        const fileReader = new FileReader();
        fileReader.readAsDataURL(file);
        fileReader.onload = () => {
            var image = new Image();
            image.src = fileReader.result as string;
            var resizedImageAsBase64 = fileReader.result as string;
            console.log("resizedImageAsBase64: " + resizedImageAsBase64.length);
            image.onload = function (e: any) {
                const MAX_WIDTH = 1024;
                // access image size here 
                console.log('image.width: ' + image.width);
                console.log('image.height: ' + image.height);
                console.log('image.src.length: ' + image.src.length);

                if (image.width > MAX_WIDTH) {
                    const canvas = document.createElement('canvas');
                    canvas.width = MAX_WIDTH;
                    canvas.height = ~~(image.height * (MAX_WIDTH / image.width));
                    const context = canvas.getContext('2d', { alpha: false });
                    if (!context) {
                        return;
                    }
                    context.drawImage(image, 0, 0, canvas.width, canvas.height);
                    resizedImageAsBase64 = canvas.toDataURL(mimeType);
                    console.log("resizedImageAsBase64: after resizing: " + resizedImageAsBase64.length);
                }
            }

            setCardImageLink(this.card, resizedImageAsBase64);
            this.updateCard();
            //lets set the state with the image value
            this.setState({
                imageLink: resizedImageAsBase64
            });
        }

        fileReader.onerror = (error) => {
            //reject(error);
        }
    }

    handleUploadClick = (event: any) => {
        if (this.fileInput.current) {
            this.fileInput.current.click();
        }
    }

    

    private deleteChoice(i: any) {
        let options = [...this.state.pollOptions];
        options.splice(i, 1);        
        setCardPollOptions(this.card, this.state.isPollMultipleChoice, options);
        this.updateCard();
        this.setState({ pollOptions: options });
    }

    private onItemChecked(i: number, checked: boolean) {
        //let checkedItem = this.state.pollOptions[i];

        let answers = this.state.pollQuizAnswers;
        if (answers && checked) {
            answers.push(i);
            
            
            setCardTitle(this.card, this.state.title);
            setCardImageLink(this.card, this.state.imageLink);
            setCardSummary(this.card, this.state.summary);
            setCardAuthor(this.card, this.state.author);
            setCardPollOptions(this.card, this.state.isPollMultipleChoice, this.state.pollOptions);

            let a: string = answers.join();
            console.log('answers in string ');
            console.log(a);
            setCardPollQuizSelectedValue(this.card, a);            

            this.setState({
                pollQuizAnswers: answers,
                card: this.card
            }, () => {
                this.updateCard();
            });
        }
        //if (answers) {
        //    let newAnswers = answers.filter(a => a !== i);
        //    this.setState({ pollQuizAnswers: newAnswers });
        //    let a: string = newAnswers.map((answer: number): number => answer).join(',')
        //    setCardPollQuizSelectedValue(this.card, a);
        //    this.updateCard();
        //}
        console.log('onItemChecked');
        console.log(this.state.pollQuizAnswers);
    }

    private updateChoiceText(i, value) {
        let options = [...this.state.pollOptions];
        options[i] = value;
        setCardPollOptions(this.card, this.state.isPollMultipleChoice, options);
        this.updateCard();
        this.setState({ pollOptions: options });
    }

    private addChoice() {
        const newValue = this.localize("PollChoice", { "choiceNumber": this.state.pollOptions.length + 1 });
        const newPollOptions = [...this.state.pollOptions, newValue];
        
        setCardTitle(this.card, this.state.title);
        setCardImageLink(this.card, this.state.imageLink);
        setCardSummary(this.card, this.state.summary);
        setCardAuthor(this.card, this.state.author);
        setCardPollOptions(this.card, this.state.isPollMultipleChoice, newPollOptions);

        this.setState({
            pollOptions: newPollOptions,
            card: this.card
        }, () => {
            this.updateCard();
        });
    }

    private onPollMultipleChoiceChanged = (event: any, data: any) => {
        setCardTitle(this.card, this.state.title);
        setCardImageLink(this.card, this.state.imageLink);
        setCardSummary(this.card, this.state.summary);
        setCardAuthor(this.card, this.state.author);
        setCardPollOptions(this.card, data.checked, this.state.pollOptions);

        this.setState({
            isPollMultipleChoice: data.checked,
            card: this.card
        }, () => {
            this.updateCard();
        });
    }

    private onPollQuizModeChanged = (event: any, data: any) => {
        this.setState({
            isPollQuizMode: data.checked,
        });
    }

    renderChoicesSection = () => {
        const choicePrefix = <CircleIcon outline size="small" className="choice-item-circle" disabled />;
        const options = this.state.pollOptions;

        let choiceOptions: any[] = [];
        let i = 0;
        options.forEach((option) => {
            const choiceOption: IChoiceContainerOption = {
                value: option,
                checked: false,
                choicePrefix: choicePrefix,
                choicePlaceholder: this.localize("PollChoice", { "choiceNumber": i + 1 }),
                deleteChoiceLabel: this.localize("PollDeleteChoiceX", { "choiceNumber": i + 1 })
            };
            choiceOptions.push(choiceOption);
            i++;
        });

        // in case we have quize mode
        const answers = this.state.pollQuizAnswers;
        console.log('const answers = this.state.pollQuizAnswers;');
        console.log(answers)
        if (answers) {
            for (let j = 0; j < answers.length; j++) {
                let answerNumber: number = answers[j];
                choiceOptions[answerNumber].checked = true;
            }
            console.log(answers);
        }

        return (
            <Flex column>
                <div className="indentation">
                    <ChoiceContainer options={choiceOptions}
                        onItemChecked={(i, checked: boolean) => {
                            this.onItemChecked(i, checked);
                        }}
                        onDeleteChoice={(item) => {
                            this.deleteChoice(item);
                        }}
                        onUpdateChoice={(item, value) => {
                            this.updateChoiceText(item, value);
                        }}
                        onAddChoice={() => {
                            this.addChoice();
                        }} />
                </div>

            </Flex>

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
            if (this.state.page === "CardCreation") {
                return (
                    <div className="taskModule">                        
                        <Flex column className="formContainer" vAlign="stretch" gap="gap.small">
                            <Flex className="scrollableContent">
                                <Flex.Item size="size.half">
                                    <Flex column className="formContentContainer">
                                        <Input className="inputField"
                                            value={this.state.title}
                                            label={this.localize("TitleText")}
                                            placeholder={this.localize("PlaceHolderTitle")}
                                            onChange={this.onTitleChanged}
                                            autoComplete="off"
                                            fluid
                                        />

                                        <Flex gap="gap.small" vAlign="end">
                                            <Input fluid className="inputField"
                                                value={this.state.imageLink}
                                                label={this.localize("ImageURL")}
                                                placeholder={this.localize("ImageURL")}
                                                onChange={this.onImageLinkChanged}
                                                error={!(this.state.errorImageUrlMessage === "")}
                                                autoComplete="off"
                                            />
                                            <Flex.Item push>
                                                <Button onClick={this.handleUploadClick}
                                                    size="medium" className="inputField"
                                                    content={this.localize("UploadImage")} iconPosition="before" />
                                            </Flex.Item>
                                            <input type="file" accept=".jpg, .jpeg, .png, .gif"
                                                style={{ display: 'none' }}
                                                multiple={false}
                                                onChange={this.handleImageSelection}
                                                ref={this.fileInput} />
                                            <Text className={(this.state.errorImageUrlMessage === "") ? "hide" : "show"} error size="small" content={this.state.errorImageUrlMessage} />
                                        </Flex>
                                        
                                        <div className="textArea">
                                            <Text content={this.localize("Summary")} />
                                            <SimpleMarkdownEditor textAreaID={"summaryTextArea"}
                                                enabledButtons={{
                                                    strike: false,
                                                    code: false,
                                                    quote: false,
                                                    h1: false,
                                                    h2: false,
                                                    h3: false,
                                                    image: false
                                                }} />
                                            <TextArea
                                                autoFocus
                                                placeholder={this.localize("Summary")}
                                                value={this.state.summary}
                                                onChange={this.onSummaryChanged}
                                                id="summaryTextArea"
                                                fluid />
                                        </div>

                                        
                                        <Input className="inputField"
                                            value={this.state.author}
                                            label={this.localize("Author")}
                                            placeholder={this.localize("Author")}
                                            onChange={this.onAuthorChanged}
                                            autoComplete="off"
                                            fluid
                                        />

                                        <Text className={(this.state.errorButtonUrlMessage === "") ? "hide" : "show"} error size="small" content={this.state.errorButtonUrlMessage} />
                                    </Flex>
                                </Flex.Item>
                                <Flex.Item size="size.half">
                                    <div className="adaptiveCardContainer">
                                    </div>
                                </Flex.Item>
                            </Flex>

                            <Flex className="footerContainer" vAlign="end" hAlign="end">
                                <Flex className="buttonContainer">
                                    <Button content={this.localize("Next")} disabled={this.isNextBtnDisabled()} id="saveBtn" onClick={this.onNext} primary />
                                </Flex>
                            </Flex>

                        </Flex>
                    </div>
                );
            }
            else if (this.state.page === "PollCreation") {
                return (
                    <div className="taskModule">
                        <Flex column className="formContainer" vAlign="stretch" gap="gap.small">
                            <Flex className="scrollableContent">
                                <Flex.Item size="size.half">
                                    <Flex column className="formContentContainer">
                                        
                                        <h3>{this.localize("PollOptions")}</h3>
                                        {this.renderChoicesSection()}
                                        <br />
                                        {/*<Checkbox label={this.localize("PollAnonymousVoting")} />*/}
                                        <Checkbox label={this.localize("PollMultipleChoice")} checked={this.state.isPollMultipleChoice} onChange={this.onPollMultipleChoiceChanged} />
                                        <Checkbox label={this.localize("PollQuizMode")} checked={this.state.isPollQuizMode} onChange={this.onPollQuizModeChanged} />

                                        <Text className={(this.state.errorButtonUrlMessage === "") ? "hide" : "show"} error size="small" content={this.state.errorButtonUrlMessage} />
                                    </Flex>
                                </Flex.Item>
                                <Flex.Item size="size.half">
                                    <div className="adaptiveCardContainer">
                                    </div>
                                </Flex.Item>
                            </Flex>

                            <Flex className="footerContainer" vAlign="end" hAlign="end">
                                <Flex className="buttonContainer">
                                    <Flex.Item push>
                                        <Button content={this.localize("Back")} onClick={this.onBack} secondary />
                                    </Flex.Item>
                                    <Button content={this.localize("Next")} disabled={this.isNextBtnDisabled()} id="saveBtn" onClick={this.onNext} primary />
                                    
                                </Flex>
                            </Flex>
                        </Flex>
                    </div>
                );
            }
            else if (this.state.page === "AudienceSelection") {
                return (
                    <div className="taskModule">
                        <Flex column className="formContainer" vAlign="stretch" gap="gap.small">
                            <Flex className="scrollableContent">
                                <Flex.Item size="size.half">
                                    <Flex column className="formContentContainer">
                                        <h3>{this.localize("SendHeadingText")}</h3>
                                        <RadioGroup
                                            className="radioBtns"
                                            checkedValue={this.state.selectedRadioBtn}
                                            onCheckedValueChange={this.onGroupSelected}
                                            vertical={true}
                                            items={[
                                                {
                                                    name: "teams",
                                                    key: "teams",
                                                    value: "teams",
                                                    label: this.localize("SendToGeneralChannel"),
                                                    children: (Component, { name, ...props }) => {
                                                        return (
                                                            <Flex key={name} column>
                                                                <Component {...props} />
                                                                <Dropdown
                                                                    hidden={!this.state.teamsOptionSelected}
                                                                    placeholder={this.localize("SendToGeneralChannelPlaceHolder")}
                                                                    search
                                                                    multiple
                                                                    items={this.getItems()}
                                                                    value={this.state.selectedTeams}
                                                                    onChange={this.onTeamsChange}
                                                                    noResultsMessage={this.localize("NoMatchMessage")}
                                                                />
                                                            </Flex>
                                                        )
                                                    },
                                                },
                                                {
                                                    name: "rosters",
                                                    key: "rosters",
                                                    value: "rosters",
                                                    label: this.localize("SendToRosters"),
                                                    children: (Component, { name, ...props }) => {
                                                        return (
                                                            <Flex key={name} column>
                                                                <Component {...props} />
                                                                <Dropdown
                                                                    hidden={!this.state.rostersOptionSelected}
                                                                    placeholder={this.localize("SendToRostersPlaceHolder")}
                                                                    search
                                                                    multiple
                                                                    items={this.getItems()}
                                                                    value={this.state.selectedRosters}
                                                                    onChange={this.onRostersChange}
                                                                    unstable_pinned={this.state.unstablePinned}
                                                                    noResultsMessage={this.localize("NoMatchMessage")}
                                                                />
                                                            </Flex>
                                                        )
                                                    },
                                                },
                                                {
                                                    name: "allUsers",
                                                    key: "allUsers",
                                                    value: "allUsers",
                                                    label: this.localize("SendToAllUsers"),
                                                    children: (Component, { name, ...props }) => {
                                                        return (
                                                            <Flex key={name} column>
                                                                <Component {...props} />
                                                                <div className={this.state.selectedRadioBtn === "allUsers" ? "" : "hide"}>
                                                                    <div className="noteText">
                                                                        <Text error content={this.localize("SendToAllUsersNote")} />
                                                                    </div>
                                                                </div>
                                                            </Flex>
                                                        )
                                                    },
                                                },
                                                {
                                                    name: "groups",
                                                    key: "groups",
                                                    value: "groups",
                                                    label: this.localize("SendToGroups"),
                                                    children: (Component, { name, ...props }) => {
                                                        return (
                                                            <Flex key={name} column>
                                                                <Component {...props} />
                                                                <div className={this.state.groupsOptionSelected && !this.state.groupAccess ? "" : "hide"}>
                                                                    <div className="noteText">
                                                                        <Text error content={this.localize("SendToGroupsPermissionNote")} />
                                                                    </div>
                                                                </div>
                                                                <Dropdown
                                                                    className="hideToggle"
                                                                    hidden={!this.state.groupsOptionSelected || !this.state.groupAccess}
                                                                    placeholder={this.localize("SendToGroupsPlaceHolder")}
                                                                    search={this.onGroupSearch}
                                                                    multiple
                                                                    loading={this.state.loading}
                                                                    loadingMessage={this.localize("LoadingText")}
                                                                    items={this.getGroupItems()}
                                                                    value={this.state.selectedGroups}
                                                                    onSearchQueryChange={this.onGroupSearchQueryChange}
                                                                    onChange={this.onGroupsChange}
                                                                    noResultsMessage={this.state.noResultMessage}
                                                                    unstable_pinned={this.state.unstablePinned}
                                                                />
                                                                <div className={this.state.groupsOptionSelected && this.state.groupAccess ? "" : "hide"}>
                                                                    <div className="noteText">
                                                                        <Text error content={this.localize("SendToGroupsNote")} />
                                                                    </div>
                                                                </div>
                                                            </Flex>
                                                        )
                                                    },
                                                }
                                            ]}
                                        >

                                        </RadioGroup>
                                        
                                        <h3>{this.localize("SendOptions")}</h3>
                                        
                                        <Checkbox label={this.localize("RequestReadReceipt")}
                                            checked={this.state.teamsOptionSelected ? false: this.state.selectedRequestReadReceipt}
                                            onChange={this.onRequestReadReceiptChanged} disabled={this.state.teamsOptionSelected} />
                                        <Checkbox label={this.localize("DelayDelivery")} checked={this.state.selectedDelayDelivery}
                                            onChange={this.onDelayDeliveryChanged} />
                                        <Flex gap="gap.smaller">
                                            <Flex.Item>
                                                <LocalizedDatePicker
                                                    screenWidth={500}
                                                    selectedDate={this.state.selectedScheduledDateTime}
                                                    minDate={new Date()}
                                                    onDateSelect={this.onDeliveryDateChanged}
                                                    disableSelection={!this.state.selectedDelayDelivery} theme={""}
                                                />
                                            </Flex.Item>
                                            <Flex.Item>
                                                <TimePicker
                                                    hours={this.state.selectedScheduledDateTime === undefined ? 0 : new Date(this.state.selectedScheduledDateTime).getHours()}
                                                    minutes={this.state.selectedScheduledDateTime === undefined ? 0 : new Date(this.state.selectedScheduledDateTime).getMinutes()}
                                                    isDisabled={!this.state.selectedDelayDelivery}
                                                    onPickerClose={this.onDeliveryTimeChange}
                                                    dir={LanguageDirection.Ltr} />
                                            </Flex.Item>                                                                                      
                                        </Flex>
                                    </Flex>
                                </Flex.Item>
                                <Flex.Item size="size.half">
                                    <div className="adaptiveCardContainer">
                                    </div>
                                    
                                </Flex.Item> 
                            </Flex>
                            <Flex className="footerContainer" vAlign="end" hAlign="end">
                                <Flex className="buttonContainer" gap="gap.small">
                                    <Flex.Item push>
                                        <Loader id="sendingLoader" className="hiddenLoader sendingLoader" size="smallest" label={this.localize("PreparingMessageLabel")} labelPosition="end" />
                                    </Flex.Item>
                                    <Flex.Item push>
                                        <Button content={this.localize("Back")} onClick={this.onBack} secondary />
                                    </Flex.Item>
                                    <Button content={this.localize("SaveAsDraft")} disabled={this.isSaveBtnDisabled()} id="saveBtn" onClick={this.onSave} primary />
                                </Flex>
                            </Flex>
                        </Flex>
                    </div>
                );
            }
            else {
                return (<div>Error</div>);
            }
        }
    }

    private onRequestReadReceiptChanged = (event: any, data: any) => {
        this.setState({
            selectedRequestReadReceipt: data.checked,
        });
    }

    private onDelayDeliveryChanged = (event: any, data: any) => {
        this.setState({
            selectedDelayDelivery: data.checked,
        })
    }

    private onDeliveryTimeChange = (hours: number, min: number) => {
        var date = this.state.selectedScheduledDateTime === undefined ?
            new Date(new Date().setHours(hours, min)) : new Date(new Date(this.state.selectedScheduledDateTime).setHours(hours, min));
        this.setState({ selectedScheduledDateTime: date });
    }

    private onDeliveryDateChanged = (date: Date) => {
        console.log(date);
        this.setState({ selectedScheduledDateTime: date });
    }


    private onGroupSelected = (event: any, data: any) => {
        this.setState({
            selectedRadioBtn: data.value,
            teamsOptionSelected: data.value === 'teams',
            rostersOptionSelected: data.value === 'rosters',
            groupsOptionSelected: data.value === 'groups',
            allUsersOptionSelected: data.value === 'allUsers',
            selectedTeams: data.value === 'teams' ? this.state.selectedTeams : [],
            selectedTeamsNum: data.value === 'teams' ? this.state.selectedTeamsNum : 0,
            selectedRosters: data.value === 'rosters' ? this.state.selectedRosters : [],
            selectedRostersNum: data.value === 'rosters' ? this.state.selectedRostersNum : 0,
            selectedGroups: data.value === 'groups' ? this.state.selectedGroups : [],
            selectedGroupsNum: data.value === 'groups' ? this.state.selectedGroupsNum : 0,
        });
    }

    private isSaveBtnDisabled = () => {
        const teamsSelectionIsValid = (this.state.teamsOptionSelected && (this.state.selectedTeamsNum !== 0)) || (!this.state.teamsOptionSelected);
        const rostersSelectionIsValid = (this.state.rostersOptionSelected && (this.state.selectedRostersNum !== 0)) || (!this.state.rostersOptionSelected);
        const groupsSelectionIsValid = (this.state.groupsOptionSelected && (this.state.selectedGroupsNum !== 0)) || (!this.state.groupsOptionSelected);
        const nothingSelected = (!this.state.teamsOptionSelected) && (!this.state.rostersOptionSelected) && (!this.state.groupsOptionSelected) && (!this.state.allUsersOptionSelected);
        return (!teamsSelectionIsValid || !rostersSelectionIsValid || !groupsSelectionIsValid || nothingSelected)
    }

    private isNextBtnDisabled = () => {
        const noSelectedChoices = (this.state.isPollQuizMode && !getQuizAnswers(this.card));

        const title = this.state.title;
        const btnTitle = this.state.btnTitle;
        const btnLink = this.state.btnLink;
        
        return !(title && ((btnTitle && btnLink) || (!btnTitle && !btnLink)) && (this.state.errorImageUrlMessage === "")
            && (this.state.errorButtonUrlMessage === "")) && noSelectedChoices;
    }

    private getItems = () => {
        const resultedTeams: dropdownItem[] = [];
        if (this.state.teams) {
            let remainingUserTeams = this.state.teams;
            if (this.state.selectedRadioBtn !== "allUsers") {
                if (this.state.selectedRadioBtn === "teams") {
                    this.state.teams.filter(x => this.state.selectedTeams.findIndex(y => y.team.id === x.id) < 0);
                }
                else if (this.state.selectedRadioBtn === "rosters") {
                    this.state.teams.filter(x => this.state.selectedRosters.findIndex(y => y.team.id === x.id) < 0);
                }
            }
            remainingUserTeams.forEach((element) => {
                resultedTeams.push({
                    key: element.id,
                    header: element.name,
                    content: element.mail,
                    image: ImageUtil.makeInitialImage(element.name),
                    team: {
                        id: element.id
                    }
                });
            });
        }
        return resultedTeams;
    }

    private static MAX_SELECTED_TEAMS_NUM: number = 20;

    private onTeamsChange = (event: any, itemsData: any) => {
        if (itemsData.value.length > NewPoll.MAX_SELECTED_TEAMS_NUM) return;
        this.setState({
            selectedTeams: itemsData.value,
            selectedTeamsNum: itemsData.value.length,
            selectedRosters: [],
            selectedRostersNum: 0,
            selectedGroups: [],
            selectedGroupsNum: 0
        })
    }

    private onRostersChange = (event: any, itemsData: any) => {
        if (itemsData.value.length > NewPoll.MAX_SELECTED_TEAMS_NUM) return;
        this.setState({
            selectedRosters: itemsData.value,
            selectedRostersNum: itemsData.value.length,
            selectedTeams: [],
            selectedTeamsNum: 0,
            selectedGroups: [],
            selectedGroupsNum: 0
        })
    }

    private onGroupsChange = (event: any, itemsData: any) => {
        this.setState({
            selectedGroups: itemsData.value,
            selectedGroupsNum: itemsData.value.length,
            groups: [],
            selectedTeams: [],
            selectedTeamsNum: 0,
            selectedRosters: [],
            selectedRostersNum: 0
        })
    }

    private onGroupSearch = (itemList: any, searchQuery: string) => {
        const result = itemList.filter(
            (item: { header: string; content: string; }) => (item.header && item.header.toLowerCase().indexOf(searchQuery.toLowerCase()) !== -1) ||
                (item.content && item.content.toLowerCase().indexOf(searchQuery.toLowerCase()) !== -1),
        )
        return result;
    }

    private onGroupSearchQueryChange = async (event: any, itemsData: any) => {

        if (!itemsData.searchQuery) {
            this.setState({
                groups: [],
                noResultMessage: "",
            });
        }
        else if (itemsData.searchQuery && itemsData.searchQuery.length <= 2) {
            this.setState({
                loading: false,
                noResultMessage: this.localize("NoMatchMessage"),
            });
        }
        else if (itemsData.searchQuery && itemsData.searchQuery.length > 2) {
            // handle event trigger on item select.
            const result = itemsData.items && itemsData.items.find(
                (item: { header: string; }) => item.header.toLowerCase() === itemsData.searchQuery.toLowerCase()
            )
            if (result) {
                return;
            }

            this.setState({
                loading: true,
                noResultMessage: "",
            });

            try {
                const query = encodeURIComponent(itemsData.searchQuery);
                const response = await searchGroups(query);
                this.setState({
                    groups: response.data,
                    loading: false,
                    noResultMessage: this.localize("NoMatchMessage")
                });
            }
            catch (error) {
                return error;
            }
        }
    }

    private onSave = () => {
        let spanner = document.getElementsByClassName("sendingLoader");
        spanner[0].classList.remove("hiddenLoader");

        const selectedTeams: string[] = [];
        const selctedRosters: string[] = [];
        const selectedGroups: string[] = [];
        this.state.selectedTeams.forEach(x => selectedTeams.push(x.team.id));
        this.state.selectedRosters.forEach(x => selctedRosters.push(x.team.id));
        this.state.selectedGroups.forEach(x => selectedGroups.push(x.team.id));

        const draftMessage: IDraftMessage = {
            id: this.state.messageId,
            title: this.state.title,
            imageLink: this.state.imageLink,
            summary: this.state.summary,
            author: this.state.author,
            buttonTitle: this.state.btnTitle,
            buttonLink: this.state.btnLink,
            teams: selectedTeams,
            rosters: selctedRosters,
            groups: selectedGroups,
            allUsers: this.state.allUsersOptionSelected,

            ack: this.state.selectedRequestReadReceipt,
            inlineTranslation: this.state.selectedInlineTranslation,
            scheduledDateTime: this.state.selectedDelayDelivery ? this.state.selectedScheduledDateTime : undefined,
            fullWidth: this.state.fullWidth,
            notifyUser: this.state.notifyUser,

            messageType: this.state.messageType,
            pollOptions: JSON.stringify(this.state.pollOptions),
            isPollQuizMode: this.state.isPollQuizMode,
            pollQuizAnswers: this.state.isPollQuizMode ? JSON.stringify(this.state.pollQuizAnswers) : "[]",
            isPollMultipleChoice: this.state.isPollMultipleChoice,
        };

        if (this.state.exists) {
            this.editDraftMessage(draftMessage).then(() => {
                microsoftTeams.tasks.submitTask();
            });
        } else {
            this.postDraftMessage(draftMessage).then(() => {
                microsoftTeams.tasks.submitTask();
            });
        }
    }

    private editDraftMessage = async (draftMessage: IDraftMessage) => {
        try {
            await updateDraftNotification(draftMessage);
        } catch (error) {
            return error;
        }
    }

    private postDraftMessage = async (draftMessage: IDraftMessage) => {
        try {
            await createDraftNotification(draftMessage);
        } catch (error) {
            throw error;
        }
    }

    public escFunction(event: any) {
        if (event.keyCode === 27 || (event.key === "Escape")) {
            microsoftTeams.tasks.submitTask();
        }
    }

    private onNext = (event: any) => {
        const current = this.state.page;
        let next: string = (current === "CardCreation") ? "PollCreation" : "AudienceSelection";

        this.setState({
            page: next
        }, () => {
            this.updateCard();
        });
    }

    private onBack = (event: any) => {
        const current = this.state.page;
        let back: string = (current === "PollCreation") ? "CardCreation" : "PollCreation";

        this.setState({
            page: back
        }, () => {
            this.updateCard();
        });
    }

    private onTitleChanged = (event: any) => {
        let showDefaultCard = (!event.target.value && !this.state.imageLink && !this.state.summary && !this.state.author && !this.state.btnTitle && !this.state.btnLink);
        setCardTitle(this.card, event.target.value);
        setCardImageLink(this.card, this.state.imageLink);
        setCardSummary(this.card, this.state.summary);
        setCardAuthor(this.card, this.state.author);
        this.setState({
            title: event.target.value,
            card: this.card
        }, () => {
            if (showDefaultCard) {
                this.setDefaultCard(this.card);
            }
            this.updateCard();
        });
    }

    private onImageLinkChanged = (event: any) => {
        let url = event.target.value.toLowerCase();
        if (!((url === "") || (url.startsWith("https://") || (url.startsWith("data:image/png;base64,")) || (url.startsWith("data:image/jpeg;base64,")) || (url.startsWith("data:image/gif;base64,"))))) {
            this.setState({
                errorImageUrlMessage: this.localize("ErrorURLMessage")
            });
        } else {
            this.setState({
                errorImageUrlMessage: ""
            });
        }

        let showDefaultCard = (!this.state.title && !event.target.value && !this.state.summary && !this.state.author && !this.state.btnTitle && !this.state.btnLink);
        setCardTitle(this.card, this.state.title);
        setCardImageLink(this.card, event.target.value);
        setCardSummary(this.card, this.state.summary);
        setCardAuthor(this.card, this.state.author);
        this.setState({
            imageLink: event.target.value,
            card: this.card
        }, () => {
            if (showDefaultCard) {
                this.setDefaultCard(this.card);
            }
            this.updateCard();
        });
    }

    private onSummaryChanged = (event: any) => {
        let showDefaultCard = (!this.state.title && !this.state.imageLink && !event.target.value && !this.state.author && !this.state.btnTitle && !this.state.btnLink);
        setCardTitle(this.card, this.state.title);
        setCardImageLink(this.card, this.state.imageLink);
        setCardSummary(this.card, event.target.value);
        setCardAuthor(this.card, this.state.author);
        this.setState({
            summary: event.target.value,
            card: this.card
        }, () => {
            if (showDefaultCard) {
                this.setDefaultCard(this.card);
            }
            this.updateCard();
        });
    }

    private onAuthorChanged = (event: any) => {
        let showDefaultCard = (!this.state.title && !this.state.imageLink && !this.state.summary && !event.target.value && !this.state.btnTitle && !this.state.btnLink);
        setCardTitle(this.card, this.state.title);
        setCardImageLink(this.card, this.state.imageLink);
        setCardSummary(this.card, this.state.summary);
        setCardAuthor(this.card, event.target.value);

        this.setState({
            author: event.target.value,
            card: this.card
        }, () => {
            if (showDefaultCard) {
                this.setDefaultCard(this.card);
            }
            this.updateCard();
        });
    }

    

    private updateCard = () => {
        const adaptiveCard = new AdaptiveCards.AdaptiveCard();
        adaptiveCard.parse(this.state.card);
        const renderedCard = adaptiveCard.render();
        const container = document.getElementsByClassName('adaptiveCardContainer')[0].firstChild;
        if (container != null) {
            container.replaceWith(renderedCard);
        } else {
            document.getElementsByClassName('adaptiveCardContainer')[0].appendChild(renderedCard);
        }
    }
}

const newPollWithTranslation = withTranslation()(NewPoll);
export default newPollWithTranslation;
