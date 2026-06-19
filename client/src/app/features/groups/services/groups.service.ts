import { inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import {
    CreateGroupRequest,
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
    readonly groups = this._groups.asReadonly();

    // Fetches groups from the API and updates the central signal state.
    // Call this from your component's ngOnInit or after actions.
    loadGroups(): void {
        this.http.get<GroupResponse[]>(this.baseUrl).subscribe({
            next: (list) => this._groups.set(list),
            error: (err) => console.error('Failed to load groups:', err)
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
}