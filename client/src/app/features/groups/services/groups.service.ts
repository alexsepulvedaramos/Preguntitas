import { inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import {
    CreateGroupRequest,
    GroupMember,
    GroupResponse,
    JoinGroupRequest,
    TransferAdminRequest,
    UpdateGroupRequest
} from '../models/group.models';
import { environment } from '../../../../environments/environment';

@Injectable({
    providedIn: 'root'
})
export class GroupsService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrl}/groups`;

    private readonly _groups = signal<GroupResponse[]>([]);
    private readonly _loading = signal(false);
    private readonly _error = signal<string | null>(null);

    readonly groups = this._groups.asReadonly();
    readonly loading = this._loading.asReadonly();
    readonly error = this._error.asReadonly();

    // Fetches groups from the API and updates the central signal state.
    // Call this from your component's ngOnInit or after actions.
    loadGroups(): void {
        this._loading.set(true);
        this._error.set(null);
        this.http.get<GroupResponse[]>(this.baseUrl).subscribe({
            next: (list) => {
                this._groups.set(list);
                this._loading.set(false);
            },
            error: () => {
                this._error.set('No se han podido cargar los grupos.');
                this._loading.set(false);
            }
        });
    }

    // Creates a new group and triggers a state refresh upon success
    createGroup(request: CreateGroupRequest): Observable<GroupResponse> {
        return this.http.post<GroupResponse>(this.baseUrl, request).pipe(
            tap(() => this.loadGroups())
        );
    }

    // Fetches details for a specific group by its ID.
    // Kept as an Observable since it represents a one-off fetch, not global state.
    getGroup(groupId: number): Observable<GroupResponse> {
        return this.http.get<GroupResponse>(`${this.baseUrl}/${groupId}`);
    }

    // Fetches the members of a group, including who the admin is
    getGroupMembers(groupId: number): Observable<GroupMember[]> {
        return this.http.get<GroupMember[]>(`${this.baseUrl}/${groupId}/members`);
    }

    // Updates basic information of an existing group
    updateGroup(groupId: number, request: UpdateGroupRequest): Observable<GroupResponse> {
        return this.http.put<GroupResponse>(`${this.baseUrl}/${groupId}`, request).pipe(
            tap(() => this.loadGroups())
        );
    }

    // Joins a group using an invitation code
    joinGroup(request: JoinGroupRequest): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/join`, request).pipe(
            tap(() => this.loadGroups())
        );
    }

    // Transfers admin privileges to another user within the group
    transferAdmin(groupId: number, request: TransferAdminRequest): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/${groupId}/admin`, request).pipe(
            // Refreshing the groups in case the UI needs to reflect the loss of admin status
            tap(() => this.loadGroups())
        );
    }

    // Leaves a group; the group is deleted server-side if the user is the last member
    leaveGroup(groupId: number): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${groupId}/members/me`).pipe(
            tap(() => this.loadGroups())
        );
    }

    // Kicks a member from a group (admin-only)
    kickMember(groupId: number, userId: number): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${groupId}/members/${userId}`);
    }

    // Generates a new invitation code for the group (admin-only)
    regenerateInviteCode(groupId: number): Observable<GroupResponse> {
        return this.http.post<GroupResponse>(`${this.baseUrl}/${groupId}/invite-code/regenerate`, {}).pipe(
            tap(() => this.loadGroups())
        );
    }
}