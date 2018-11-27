import { Observable, Subscription } from 'rxjs';

import { Component, OnDestroy, OnInit } from '@angular/core';
import { select, Store } from '@ngrx/store';

import { MessageModel } from '../../../../core/services/directline/models/activity-model';
import { AppState } from '../../../../reducers';
import { ActionPlan } from '../../../../reducers/action-plan/action-plan.model';
import { actionPlansSelector } from '../../../../reducers/action-plan/action-plan.selectors';
import { SetPageData } from '../../../../reducers/app/app.actions';
import {
    SelectActiveConversation, ToggleAllUsersChatWindow, ToggleUserChatWindow
} from '../../../../reducers/chat/chat.actions';
import {
    chatActiveUsersSelector, chatMessagesSelector
} from '../../../../reducers/chat/chat.selectors';
import { Event } from '../../../../reducers/event/event.model';
import { activeMobileEventsSelector } from '../../../../reducers/event/event.selectors';

@Component({
    selector: 'app-recently-active',
    templateUrl: './recently-active.component.html',
    styleUrls: [ './recently-active.component.scss' ],
})
export class RecentlyActiveComponent implements OnInit, OnDestroy {
    activeUsers$: Observable<any[]>;
    actionPlansSub$: Subscription;
    activeMobileEvents$: Subscription;
    messagesSub$: Subscription;
    actionPlans: ActionPlan[];
    events: Event[];
    messages: MessageModel[];

    constructor (private store: Store<AppState>) { }

    ngOnInit() {
        this.store.dispatch(new SetPageData({ title: 'Messaging', sidebar: true }));
        this.activeUsers$ = this.store.pipe(select(chatActiveUsersSelector));
        this.actionPlansSub$ = this.store.pipe(select(actionPlansSelector)).subscribe(actionPlans => this.actionPlans = actionPlans);
        this.messagesSub$ = this.store.pipe(select(chatMessagesSelector)).subscribe((messages: MessageModel[]) => this.messages = messages);

        this.activeMobileEvents$ = this.store
            .pipe(select(activeMobileEventsSelector))
            .subscribe(events => this.events = events);
    }

    ngOnDestroy() {
        this.actionPlansSub$.unsubscribe();
        this.messagesSub$.unsubscribe();
    }

    getUnreadMessageCount(userId: string) {
        return this.messages.filter(m => m.channelData.data.userId === userId && !m.read).length;
    }

    getActionPlanIcon(userId: string) {
        const actionPlanId = this.getLatestActionPlanId(userId);
        if (actionPlanId) {
            const actionPlan = this.actionPlans.find(ap => ap.actionPlanId === actionPlanId);
            if (actionPlan) {
                return `${actionPlan.color} ${actionPlan.icon}-static round large-medium app-icon`;
            }
        }

        return null;
    }

    getLatestActionPlanId(userId: string) {
        const event = this.events.find(event => event.events.some(ee => ee.metadata.userId === userId));
        if (event) {
            const latestEventInstance = event.events
                .sort((a, b) => (new Date(b.date).getTime() - new Date(a.date).getTime()))[ 0 ];
            if (latestEventInstance) {
                return latestEventInstance.metadata.reportType;
            }
        }

        return null;
    }

    selectActiveUser(user) {
        this.store.dispatch(new ToggleUserChatWindow({ open: true, userId: user.id, userName: user.name }));
        this.store.dispatch(new SelectActiveConversation({ conversationId: user.conversationId }));
    }

    selectAllChat() {
        this.store.dispatch(new ToggleAllUsersChatWindow({ open: true, userId: '*' }));
    }
}
