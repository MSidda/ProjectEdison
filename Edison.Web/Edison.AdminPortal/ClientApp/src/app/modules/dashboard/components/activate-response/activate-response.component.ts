import { Subscription } from 'rxjs';

import { Component, OnDestroy, OnInit } from '@angular/core';
import { Actions, ofType } from '@ngrx/effects';
import { select, Store } from '@ngrx/store';

import { fadeInOut } from '../../../../core/animations/fadeInOut';
import { SearchListItem } from '../../../../core/models/searchListItem';
import { AppState } from '../../../../reducers';
import {
    GetActionPlan, GetActionPlans, PutActionPlan, SelectActionPlan, SetSelectingActionPlan
} from '../../../../reducers/action-plan/action-plan.actions';
import { ActionPlan, ActionStatus } from '../../../../reducers/action-plan/action-plan.model';
import {
    actionPlansSelector, selectedActionPlanSelector
} from '../../../../reducers/action-plan/action-plan.selectors';
import { SelectActiveEvent } from '../../../../reducers/event/event.actions';
import { Event } from '../../../../reducers/event/event.model';
import {
    PostNewResponse, ResponseActionTypes, RetryResponseActions, ShowActivateResponse
} from '../../../../reducers/response/response.actions';
import { Response } from '../../../../reducers/response/response.model';
import { activeResponseSelector } from '../../../../reducers/response/response.selectors';

@Component({
    selector: 'app-activate-response',
    templateUrl: './activate-response.component.html',
    styleUrls: [ './activate-response.component.scss' ],
    animations: [ fadeInOut ],
})
export class ActivateResponseComponent implements OnInit, OnDestroy {
    hover: boolean;
    active = false;
    disabled = false;
    selectedActionPlan: ActionPlan = null;
    showActionPlan = false;
    activeEvent: Event;
    activeResponse: Response;
    listItems: SearchListItem[];
    actionPlans: ActionPlan[];
    activated = false;
    loadingFullActionPlan = false;
    responseNeedsLocation: boolean;

    actionPlanPartialSuccess: boolean;
    actionPlanSuccessful: boolean;
    actionPlanHasErrors: boolean;
    actionPlanNeedsLocation: boolean;

    private actionPlansSub$: Subscription;
    private activeEventSub$: Subscription;
    private responsesSub$: Subscription;

    constructor (private store: Store<AppState>, private actions$: Actions) { }

    ngOnInit() {
        this.initSubscriptions();
        this.updateResponseStatus()
        this.store.dispatch(new GetActionPlans())
    }

    ngOnDestroy() {
        this.actionPlansSub$.unsubscribe()
        this.activeEventSub$.unsubscribe()
        this.responsesSub$.unsubscribe()
    }

    initSubscriptions() {
        this.actionPlansSub$ = this.store
            .pipe(select(actionPlansSelector))
            .subscribe(actionPlans => {
                this.listItems = actionPlans.map(ap => ({
                    name: ap.name,
                    id: ap.actionPlanId,
                    icon: ap.icon || '',
                    color: ap.color || '',
                }))
                this.actionPlans = actionPlans
            })

        this.actionPlansSub$ = this.store
            .pipe(select(selectedActionPlanSelector))
            .subscribe(actionPlan => {
                if (actionPlan) {
                    this._updateActionPlan(actionPlan);
                    if (this.loadingFullActionPlan &&
                        actionPlan.openActions &&
                        actionPlan.openActions.length > 0) {
                        this.loadingFullActionPlan = false;
                    }
                    this.responseNeedsLocation = !this.activeEvent;
                } else {
                    this.showActionPlan = false;
                }
            })

        this.activeEventSub$ = this.actions$
            .pipe(ofType(ResponseActionTypes.ShowActivateResponse))
            .subscribe(({ payload: { event, actionPlanId } }: ShowActivateResponse) => {
                this.active = true;
                this.activated = false;
                this.activeEvent = event;

                if (actionPlanId) { this._selectActionPlan(actionPlanId); }

                this.store.dispatch(new SetSelectingActionPlan({ isSelecting: this.active }))
            })

        this.responsesSub$ = this.store
            .pipe(select(activeResponseSelector))
            .subscribe(({ activeResponse }) => {
                this.activeResponse = activeResponse;
                if (activeResponse) {
                    this._updateActionPlan(this.activeResponse.actionPlan);
                }
                this.updateResponseStatus();
            })
    }

    updateResponseStatus() {
        if (this.selectedActionPlan && this.selectedActionPlan.openActions) {
            this.actionPlanSuccessful = !this.selectedActionPlan.openActions.some(action => action.status !== ActionStatus.Success);
            this.actionPlanNeedsLocation = this.responseNeedsLocation && this.selectedActionPlan.openActions.some(action => action.status === ActionStatus.Skipped);
            this.actionPlanHasErrors = this.selectedActionPlan.openActions.some(action => action.status === ActionStatus.Error || action.status === ActionStatus.Unknown);
            this.actionPlanPartialSuccess = this.selectedActionPlan.openActions.some(action => action.status === ActionStatus.Success);
        } else {
            this.actionPlanSuccessful = false;
            this.actionPlanNeedsLocation = false;
            this.actionPlanHasErrors = false;
            this.actionPlanPartialSuccess = false;
        }
    }

    onActionPlanChange() {
        // console.log(this.selectedActionPlan);
    }

    selectActionPlan = (item: SearchListItem) => {
        if (item) {
            this._selectActionPlan(item.id);
        } else {
            this.store.dispatch(new SelectActionPlan({ actionPlan: null }))
        }
    }

    private _updateActionPlan(actionPlan: ActionPlan) {
        if (this.selectedActionPlan && this.selectedActionPlan.openActions && actionPlan.openActions) {
            this.selectedActionPlan = {
                ...actionPlan,
                openActions: actionPlan.openActions.map(action => ({
                    ...action,
                    loading: this.selectedActionPlan.openActions.some(oa => oa.actionId === action.actionId && oa.loading),
                }))
            }
        } else {
            this.selectedActionPlan = actionPlan;
        }
    }

    private _selectActionPlan(actionPlanId: string) {
        const actionPlan = this.actionPlans.find(ap => ap.actionPlanId === actionPlanId)
        if (!actionPlan.openActions) {
            this.loadingFullActionPlan = true;
            this.store.dispatch(new GetActionPlan({ actionPlanId: actionPlan.actionPlanId }));
        } else if (!this.activeEvent) {
            this.responseNeedsLocation = true;
        }
        this.showActionPlan = true

        this.store.dispatch(new SelectActionPlan({ actionPlan }))
    }

    activateActionPlan = () => {
        this.setActionsLoading();
        this.store.dispatch(
            new PostNewResponse({
                event: this.activeEvent,
                actionPlan: this.selectedActionPlan,
            })
        )
        this.store.dispatch(new PutActionPlan({ actionPlan: this.selectedActionPlan }));
        this.activated = true;
    }

    retry() {
        if (this.activeResponse) {
            this.store.dispatch(new RetryResponseActions({ responseId: this.activeResponse.responseId }));
            this.setActionsLoading();
        }
    }

    setActionsLoading() {
        if (this.selectedActionPlan && this.selectedActionPlan.openActions) {
            this.selectedActionPlan.openActions
                .filter(action => action.status !== ActionStatus.Success)
                .forEach(action => action.loading = true);
        }
    }

    onReturnToMapClick() {
        this.toggleActive()
    }

    toggleActive() {
        if (this.disabled) { return; }

        if (this.active) {
            this.active = false
            this.activated = false
        } else {
            this.active = true;
        }

        this.store.dispatch(new SelectActionPlan({ actionPlan: null }));
        this.store.dispatch(new SelectActiveEvent({ event: null }));
        this.store.dispatch(new SetSelectingActionPlan({ isSelecting: this.active }));
    }
}
