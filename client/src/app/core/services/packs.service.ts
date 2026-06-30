import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { Pack, PackTemplatePage } from '../models/pack.model';
import { QuestionType } from '../enums/question-type.enum';
import { environment } from '../../../environments/environment';

// Packs available to a group: list with per-group enablement, admin-only toggle,
// and paginated browsing of a pack's templates for the daily-question picker.
@Injectable({
    providedIn: 'root'
})
export class PacksService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrl}/groups`;

    // Packs with their effective enablement for this group
    getPacks(groupId: number): Observable<Pack[]> {
        return this.http.get<Pack[]>(`${this.baseUrl}/${groupId}/packs`);
    }

    // Enables/disables a pack for the group (admin-only; backend rejects leaving zero enabled)
    setPackEnabled(groupId: number, packId: number, enabled: boolean): Observable<void> {
        return this.http.patch<void>(`${this.baseUrl}/${groupId}/packs/${packId}`, { enabled });
    }

    // Templates available to the selector, paginated (cursor-based) and filterable by
    // type/pack. Pass `before` (last seen template id) for subsequent pages.
    getTemplates(
        groupId: number,
        opts: { type?: QuestionType; packId?: number; before?: number; pageSize?: number } = {},
    ): Observable<PackTemplatePage> {
        const params: Record<string, string> = {};
        if (opts.type !== undefined) params['type'] = String(opts.type);
        if (opts.packId !== undefined) params['packId'] = String(opts.packId);
        if (opts.before !== undefined) params['before'] = String(opts.before);
        if (opts.pageSize !== undefined) params['pageSize'] = String(opts.pageSize);
        return this.http.get<PackTemplatePage>(`${this.baseUrl}/${groupId}/packs/templates`, { params });
    }
}
