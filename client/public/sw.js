// Vaya Preguntita service worker: Angular's ngsw (caching, updates, notification clicks)
// plus our own push handling, which ngsw can't do:
//  - one notification per group: new pushes for a group merge into the one already
//    showing ("Ana, Luis y 2 más han votado") instead of piling up;
//  - nothing is shown while the app is open and visible.
// Our listener is registered before ngsw's, so stopImmediatePropagation() keeps ngsw
// from showing its own copy. Payloads without our `data.kind` fall through to ngsw.

const MAX_EVENTS = 30;

// WebKit revokes push subscriptions whose pushes don't show a notification, so there we
// still show it while the app is visible — and close it straight away.
const MUST_SHOW_EVERY_PUSH = /AppleWebKit/.test(self.navigator.userAgent)
  && !/Chrome|Chromium|Android/.test(self.navigator.userAgent);

self.addEventListener('push', (event) => {
  let payload;
  try {
    payload = event.data ? event.data.json() : null;
  } catch {
    return;
  }
  const notification = payload && payload.notification;
  if (!notification || !notification.data || !notification.data.kind) return;

  event.stopImmediatePropagation();
  event.waitUntil(showGrouped(notification));
});

async function showGrouped(notification) {
  const { title, ...options } = notification;
  const data = options.data;
  const tag = `group-${data.groupId}`;

  const windows = await self.clients.matchAll({ type: 'window', includeUncontrolled: true });
  const appVisible = windows.some((w) => w.visibilityState === 'visible');
  if (appVisible && !MUST_SHOW_EVERY_PUSH) return;

  const showing = await self.registration.getNotifications({ tag });
  const previous = (showing[0] && showing[0].data && showing[0].data.events) || [];
  const events = mergeEvents(previous, { kind: data.kind, actor: data.actor, title, body: options.body });

  const merged = events.length > 1;
  await self.registration.showNotification(merged ? data.groupName : title, {
    ...options,
    body: merged ? summarize(events, TEXT[data.lang] || TEXT.es) : options.body,
    tag,
    renotify: showing.length > 0,
    data: { ...data, events },
  });

  if (appVisible) {
    const shown = await self.registration.getNotifications({ tag });
    shown.forEach((n) => n.close());
  }
}

function mergeEvents(previous, event) {
  // A new question starts a new cycle: votes on the previous one and the race to pick it
  // (rama 40) no longer matter.
  // A daily duel (rama 47) is the new question for its two sides.
  const stale = ['user_voted', 'new_question', 'duel', 'race_to_select'];
  const kept = event.kind === 'new_question' || event.kind === 'duel'
    ? previous.filter((e) => !stale.includes(e.kind))
    : previous;
  return [...kept, event].slice(-MAX_EVENTS);
}

// The merged summary's wording, in the recipient's language (`data.lang`, rama 23).
const TEXT = {
  es: {
    selectorTurn: '¡Te toca elegir la pregunta!',
    raceToSelect: '¡Elige tú la próxima pregunta!',
    flash: (name) => `Pregunta flash de ${name}`,
    summary: 'Ya está el resumen del mes',
    voted: (names, count) => `${names} ${count === 1 ? 'ha' : 'han'} votado`,
    messages: (count, names) => `${count} mensajes nuevos de ${names}`,
    and: 'y',
    more: (count) => `${count} más`,
  },
  en: {
    selectorTurn: "It's your turn to pick the question!",
    raceToSelect: 'Your pick: the next question is up for grabs!',
    flash: (name) => `Flash question from ${name}`,
    summary: 'Your monthly summary is here',
    voted: (names) => `${names} voted`,
    messages: (count, names) => `${count} new messages from ${names}`,
    and: 'and',
    more: (count) => `${count} more`,
  },
  de: {
    selectorTurn: 'Du bist dran, die Frage auszuwählen!',
    raceToSelect: 'Du darfst die nächste Frage wählen!',
    flash: (name) => `Blitzfrage von ${name}`,
    summary: 'Dein Monatsrückblick ist da',
    voted: (names, count) => `${names} ${count === 1 ? 'hat' : 'haben'} abgestimmt`,
    messages: (count, names) => `${count} neue Nachrichten von ${names}`,
    and: 'und',
    more: (count) => `${count} weitere`,
  },
};

function summarize(events, text) {
  const lines = [];
  const latest = (kind) => [...events].reverse().find((e) => e.kind === kind);

  const danger = latest('streak_danger');
  if (danger) lines.push(`🔥 ${danger.body}`);

  if (latest('selector_turn')) lines.push(`🎯 ${text.selectorTurn}`);
  if (latest('race_to_select')) lines.push(`⏱️ ${text.raceToSelect}`);

  // Monthly summary (rama 41): sent with the 1st's question, so it usually merges with it.
  if (latest('monthly_summary')) lines.push(`🎉 ${text.summary}`);

  const question = latest('new_question');
  if (question) lines.push(`❓ ${question.body}`);

  // Duels (rama 47): the daily one or a flash, each listed with who set it up.
  events.filter((e) => e.kind === 'duel' || e.kind === 'flash_duel').forEach((e) => lines.push(`⚔️ ${e.body}`));

  // Flash questions (rama 44) live 24 h on their own, so every one stays listed.
  const flashes = events.filter((e) => e.kind === 'flash_question');
  if (flashes.length === 1) lines.push(`⚡ ${flashes[0].body}`);
  else flashes.forEach((e) => lines.push(`⚡ ${text.flash(e.actor)}`));

  const voters = unique(events.filter((e) => e.kind === 'user_voted').map((e) => e.actor));
  if (voters.length) lines.push(`🗳️ ${text.voted(listNames(voters, text), voters.length)}`);

  const messages = events.filter((e) => e.kind === 'new_message');
  if (messages.length === 1) {
    lines.push(`💬 ${messages[0].body}`);
  } else if (messages.length > 1) {
    const senders = unique(messages.map((e) => e.actor));
    lines.push(`💬 ${text.messages(messages.length, listNames(senders, text))}`);
  }

  return lines.join('\n');
}

function unique(names) {
  return [...new Set(names.filter(Boolean))];
}

function listNames(names, text) {
  if (names.length === 1) return names[0];
  if (names.length === 2) return `${names[0]} ${text.and} ${names[1]}`;
  if (names.length === 3) return `${names[0]}, ${names[1]} ${text.and} ${names[2]}`;
  return `${names[0]}, ${names[1]} ${text.and} ${text.more(names.length - 2)}`;
}

importScripts('./ngsw-worker.js');
