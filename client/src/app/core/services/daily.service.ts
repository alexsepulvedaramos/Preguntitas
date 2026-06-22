import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { SelectionSources } from '../models/daily.model';
import { environment } from '../../../environments/environment';

// Daily flow within a group: selection sources today, and (ramas 3/5) current state,
// question selection and voting.
@Injectable({
    providedIn: 'root'
})
export class DailyService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrl}/groups`;

    // Fetches the questions the day's selector can pick from: the group pool + the base pack
    getSelectionSources(groupId: number): Observable<SelectionSources> {
        return this.http.get<SelectionSources>(`${this.baseUrl}/${groupId}/daily/selection-sources`);
    }
}
