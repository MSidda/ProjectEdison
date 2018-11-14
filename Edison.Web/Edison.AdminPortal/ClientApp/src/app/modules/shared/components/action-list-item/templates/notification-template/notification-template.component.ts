import {
    Component,
    Input,
    ChangeDetectionStrategy,
    EventEmitter,
    OnInit,
    ElementRef,
    ViewChild,
    AfterViewInit,
} from '@angular/core'
import {
    ActionPlanNotificationAction,
    ActionChangeType,
    AddEditAction
} from '../../../../../../reducers/action-plan/action-plan.model'

@Component({
    selector: 'app-notification-template',
    templateUrl: './notification-template.component.html',
    styleUrls: [ './notification-template.component.scss' ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationTemplateComponent implements OnInit, AfterViewInit {
    @ViewChild('textarea') textarea: ElementRef;

    @Input() context: ActionPlanNotificationAction;
    @Input() last: boolean;
    @Input() canEdit: boolean;
    @Input() onchange: EventEmitter<{ actionId: string, addEditAction: AddEditAction }>;

    notificationText: string;
    editing = false;
    adding = false;

    ngOnInit(): void {
        this.notificationText = this.context.parameters.message;
        if (this.context.parameters.editing) {
            this.editing = true;
            this.adding = true;
        } else {
            this.editing = false;
            this.adding = false;
        }
    }

    ngAfterViewInit() {
        if (this.context.parameters.editing) {
            this.textarea.nativeElement.scrollIntoView();
            this.textarea.nativeElement.focus();
            this.context.parameters.editing = false;
        }
    }

    addNew() {
        this.notificationText = '';
        this.adding = true;
    }

    remove() {
        this.notificationText = '';
        this.adding = false;
    }

    edit() {
        this.editing = true;
        setTimeout(() => { this.textarea.nativeElement.focus(); }); // kick this event to the end of the line
    }

    editComplete() {
        this.editing = false;
    }

    notificationChanged() {
        if (!this.canEdit) {
            return;
        }

        let actionChangeType = this.adding ? ActionChangeType.Add : ActionChangeType.Edit;
        if (this.notificationText.trim() === '') {
            actionChangeType = ActionChangeType.Delete;
        }

        const addEditAction: AddEditAction = {
            actionChangedString: actionChangeType,
            isCloseAction: true,
            action: {
                actionId: actionChangeType === ActionChangeType.Delete || actionChangeType === ActionChangeType.Edit ? this.context.actionId : null,
                actionType: this.context.actionType,
                isActive: this.context.isActive,
                description: this.context.description,
                parameters: {
                    message: this.notificationText
                }
            }
        };

        this.onchange.emit({
            actionId: this.context.actionId,
            addEditAction,
        });
    }
}
