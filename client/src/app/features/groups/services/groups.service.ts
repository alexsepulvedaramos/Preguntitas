import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
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
    private readonly baseUrl = `${environment.apiUrl}/api/groups`;

    constructor(private http: HttpClient) { }

    // Retrieves all groups associated with the authenticated user
    getUserGroups(): Observable<GroupResponse[]> {
        return this.http.get<GroupResponse[]>(this.baseUrl);
    }

    // Creates a new group
    createGroup(request: CreateGroupRequest): Observable<GroupResponse> {
        return this.http.post<GroupResponse>(this.baseUrl, request);
    }

    // Fetches details for a specific group by its ID
    getGroup(groupId: number): Observable<GroupResponse> {
        return this.http.get<GroupResponse>(`${this.baseUrl}/${groupId}`);
    }

    // Updates basic information of an existing group
    updateGroup(groupId: number, request: UpdateGroupRequest): Observable<GroupResponse> {
        return this.http.put<GroupResponse>(`${this.baseUrl}/${groupId}`, request);
    }

    // Joins a group using an invitation code
    joinGroup(request: JoinGroupRequest): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/join`, request);
    }

    // Transfers admin privileges to another user within the group
    transferAdmin(groupId: number, request: TransferAdminRequest): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/${groupId}/admin`, request);
    }
}