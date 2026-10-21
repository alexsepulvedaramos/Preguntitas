import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { GroupChatComponent, CHAT_POLL_INTERVAL_MS } from './group-chat.component';
import { DailyService } from '../../../../core/services/daily.service';
import { ChatMessage } from '../../../../core/models/chat-message.model';

const message = (id: number): ChatMessage => ({
  id,
  body: `m${id}`,
  createdAt: '2026-10-12T10:00:00Z',
  userId: 1,
  username: 'ana',
  isSystem: false,
  isCurrentUser: false,
  reactions: [],
});

describe('GroupChatComponent live refresh', () => {
  let visibility: DocumentVisibilityState;
  const getChat = vi.fn();

  const setVisibility = (value: DocumentVisibilityState) => {
    visibility = value;
    document.dispatchEvent(new Event('visibilitychange'));
  };

  const create = () => {
    TestBed.configureTestingModule({
      imports: [GroupChatComponent],
      providers: [{ provide: DailyService, useValue: { getChat } }],
    });
    const fixture = TestBed.createComponent(GroupChatComponent);
    fixture.componentRef.setInput('groupId', 1);
    fixture.detectChanges();
    return fixture;
  };

  beforeEach(() => {
    vi.useFakeTimers();
    visibility = 'visible';
    vi.spyOn(document, 'visibilityState', 'get').mockImplementation(() => visibility);
    vi.spyOn(window, 'scrollTo').mockImplementation(() => {});
    getChat.mockReset();
    getChat.mockReturnValue(of([message(1)]));
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.restoreAllMocks();
  });

  it('re-fetches every 15 s while visible', () => {
    const fixture = create();
    expect(getChat).toHaveBeenCalledTimes(1);
    vi.advanceTimersByTime(CHAT_POLL_INTERVAL_MS * 2);
    expect(getChat).toHaveBeenCalledTimes(3);
    fixture.destroy();
  });

  it('makes no requests while hidden and refreshes once when visible again', () => {
    const fixture = create();
    setVisibility('hidden');
    vi.advanceTimersByTime(CHAT_POLL_INTERVAL_MS * 4);
    expect(getChat).toHaveBeenCalledTimes(1);

    setVisibility('visible');
    vi.advanceTimersByTime(0);
    expect(getChat).toHaveBeenCalledTimes(2);
    fixture.destroy();
  });

  it('stops polling when destroyed', () => {
    const fixture = create();
    fixture.destroy();
    vi.advanceTimersByTime(CHAT_POLL_INTERVAL_MS * 3);
    expect(getChat).toHaveBeenCalledTimes(1);
  });

  it('keeps the typed message when new messages arrive', () => {
    const fixture = create();
    const chat = fixture.componentInstance as any;
    chat.messageText = 'draft';
    getChat.mockReturnValue(of([message(1), message(2)]));
    vi.advanceTimersByTime(CHAT_POLL_INTERVAL_MS);
    expect(chat.messages().length).toBe(2);
    expect(chat.messageText).toBe('draft');
    fixture.destroy();
  });
  it('picks up reactions that arrive without new messages', () => {
    const fixture = create();
    const chat = fixture.componentInstance as any;
    getChat.mockReturnValue(of([{ ...message(1), reactions: [{ emoji: 'fire', userId: 2, username: 'bea' }] }]));
    vi.advanceTimersByTime(CHAT_POLL_INTERVAL_MS);
    expect(chat.messages()[0].reactions).toEqual([{ emoji: 'fire', userId: 2, username: 'bea' }]);
    fixture.destroy();
  });
});
