using System.Globalization;

namespace VayaPreguntita.API.Localization;

// Player-facing text the API produces (rama 23). Request messages answer in the UI language
// the client sends as Accept-Language (→ CurrentUICulture, see UseRequestLocalization in
// Program.cs); push notifications are written in each recipient's PreferredLanguage.
// Developer-facing contract errors (structural validator rules the UI never lets through)
// stay in English. Pack questions, streak titles and chat system messages are content —
// rama 24 — and are not here.
public static class ServerText
{
    private static string RequestLanguage =>
        AppLanguages.Normalize(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);

    private static string Pick(string es, string en, string de) => Pick(RequestLanguage, es, en, de);

    private static string Pick(string lang, string es, string en, string de) => lang switch
    {
        AppLanguages.English => en,
        AppLanguages.German => de,
        _ => es,
    };

    // ── Auth & account ──────────────────────────────────────────────────────────────────
    public static string RegistrationFieldsRequired => Pick(
        "El usuario, el email y la contraseña son obligatorios.",
        "Username, email and password are required.",
        "Benutzername, E-Mail und Passwort sind Pflichtfelder.");

    public static string UsernameTaken => Pick(
        "Ese nombre de usuario ya está en uso.",
        "That username is already taken.",
        "Dieser Benutzername ist schon vergeben.");

    public static string EmailTaken => Pick(
        "Ese email ya está registrado.",
        "That email is already registered.",
        "Diese E-Mail ist schon registriert.");

    public static string LoginFieldsRequired => Pick(
        "Escribe tu usuario o email y tu contraseña.",
        "Enter your username or email and your password.",
        "Gib deinen Benutzernamen oder deine E-Mail und dein Passwort ein.");

    public static string InvalidCredentials => Pick(
        "Usuario o contraseña incorrectos.",
        "Wrong username or password.",
        "Benutzername oder Passwort ist falsch.");

    public static string RefreshTokenRequired => Pick(
        "Falta el token de sesión.",
        "The session token is missing.",
        "Das Sitzungstoken fehlt.");

    public static string InvalidRefreshToken => Pick(
        "La sesión ha caducado. Vuelve a entrar.",
        "Your session has expired. Please sign in again.",
        "Deine Sitzung ist abgelaufen. Melde dich erneut an.");

    public static string UserNotFound => Pick(
        "No se ha encontrado el usuario.",
        "User not found.",
        "Benutzer nicht gefunden.");

    public static string WrongPassword => Pick(
        "La contraseña actual es incorrecta.",
        "Your current password is incorrect.",
        "Dein aktuelles Passwort ist falsch.");

    public static string PasswordsRequired => Pick(
        "Escribe la contraseña actual y la nueva.",
        "Enter your current and your new password.",
        "Gib dein aktuelles und dein neues Passwort ein.");

    public static string NewPasswordTooShort => Pick(
        "La nueva contraseña debe tener al menos 8 caracteres.",
        "Your new password must be at least 8 characters long.",
        "Dein neues Passwort muss mindestens 8 Zeichen lang sein.");

    public static string InvalidFrameColor => Pick(
        "Ese color de marco no es válido.",
        "That frame colour isn't valid.",
        "Diese Rahmenfarbe ist ungültig.");

    public static string TitleNotUnlocked => Pick(
        "Aún no has desbloqueado ese título.",
        "You haven't unlocked that title yet.",
        "Diesen Titel hast du noch nicht freigeschaltet.");

    public static string InvalidLanguage => Pick(
        "Ese idioma no está disponible.",
        "That language isn't available.",
        "Diese Sprache ist nicht verfügbar.");

    public static string NoFileProvided => Pick(
        "No se ha enviado ninguna imagen.",
        "No image was sent.",
        "Es wurde kein Bild gesendet.");

    public static string FileTooLarge => Pick(
        "La imagen no puede pasar de 5 MB.",
        "The image can't be larger than 5 MB.",
        "Das Bild darf höchstens 5 MB groß sein.");

    public static string InvalidImageType => Pick(
        "Formato de imagen no válido (usa JPG, PNG, WebP o GIF).",
        "Invalid image format (use JPG, PNG, WebP or GIF).",
        "Ungültiges Bildformat (verwende JPG, PNG, WebP oder GIF).");

    // ── Groups ──────────────────────────────────────────────────────────────────────────
    public static string InvalidInvitationCode => Pick(
        "Código de invitación inválido o grupo no encontrado.",
        "Invalid invitation code or group not found.",
        "Ungültiger Einladungscode oder Gruppe nicht gefunden.");

    public static string UseLeaveGroup => Pick(
        "Para salir tú del grupo, usa «Salir del grupo».",
        "To remove yourself, use “Leave group”.",
        "Um selbst zu gehen, nutze „Gruppe verlassen“.");

    public static string CannotKickMember => Pick(
        "No se ha encontrado al miembro o es el administrador.",
        "Member not found, or they're the admin.",
        "Mitglied nicht gefunden oder es ist der Admin.");

    public static string AdminTargetNotMember => Pick(
        "El nuevo administrador tiene que ser miembro del grupo.",
        "The new admin must be a member of the group.",
        "Der neue Admin muss Mitglied der Gruppe sein.");

    public static string InvalidStreakLeadTime => Pick(
        "Esa antelación para el aviso de racha no es válida.",
        "That streak reminder time isn't valid.",
        "Diese Vorlaufzeit für die Serien-Erinnerung ist ungültig.");

    // ── Daily question ──────────────────────────────────────────────────────────────────
    public static string QuestionNotInPool => Pick(
        "Pregunta no encontrada en el pool.",
        "Question not found in the pool.",
        "Frage nicht im Pool gefunden.");

    public static string QuestionNotValidForGroup => Pick(
        "La pregunta no es válida para este grupo.",
        "This question isn't valid for this group.",
        "Diese Frage ist für diese Gruppe nicht gültig.");

    public static string QuestionAlreadyActive => Pick(
        "La pregunta ya está activa y no se puede cambiar.",
        "The question is already live and can't be changed.",
        "Die Frage ist schon aktiv und kann nicht mehr geändert werden.");

    public static string TemplateAlreadyUsed => Pick(
        "Esta pregunta del pack ya se ha usado en este grupo. Elige otra.",
        "This pack question has already been used in this group. Pick another one.",
        "Diese Pack-Frage wurde in dieser Gruppe schon gestellt. Wähl eine andere.");

    public static string NoActiveQuestionToday => Pick(
        "No hay pregunta activa hoy.",
        "There's no live question today.",
        "Heute gibt es keine aktive Frage.");

    public static string AlreadyVoted => Pick(
        "Ya has votado hoy.",
        "You've already voted today.",
        "Du hast heute schon abgestimmt.");

    public static string AlreadyVotedThis => Pick(
        "Ya has votado esta pregunta.",
        "You've already voted on this question.",
        "Du hast bei dieser Frage schon abgestimmt.");

    public static string LateVoteNotClosed => Pick(
        "Esta pregunta sigue abierta: vótala desde la pantalla del grupo.",
        "This question is still open: vote on it from the group screen.",
        "Diese Frage ist noch offen: Stimm im Gruppenbildschirm ab.");

    public static string InvalidVote => Pick(
        "Voto inválido para este tipo de pregunta.",
        "Invalid vote for this type of question.",
        "Ungültige Stimme für diesen Fragetyp.");

    // ── Questions & packs ───────────────────────────────────────────────────────────────
    public static string NoQuestionForDate => Pick(
        "No hay pregunta para esa fecha.",
        "There's no question for that date.",
        "Für dieses Datum gibt es keine Frage.");

    public static string QuestionNotInGroup => Pick(
        "La pregunta no existe en este grupo.",
        "That question doesn't exist in this group.",
        "Diese Frage gibt es in dieser Gruppe nicht.");

    public static string QuestionAlreadyUsed => Pick(
        "No se puede eliminar una pregunta que ya se ha usado.",
        "A question that has already been used can't be deleted.",
        "Eine Frage, die schon gestellt wurde, kann nicht gelöscht werden.");

    public static string UnusedQuestionsLimit(int max) => Pick(
        $"Ya tienes {max} preguntas sin usar en el pool de este grupo.",
        $"You already have {max} unused questions in this group's pool.",
        $"Du hast schon {max} ungenutzte Fragen im Pool dieser Gruppe.");

    public static string GroupPoolFull => Pick(
        "El pool de preguntas de este grupo está lleno.",
        "This group's question pool is full.",
        "Der Fragen-Pool dieser Gruppe ist voll.");

    public static string CreatorBlacklisted => Pick(
        "El creador no puede estar en la lista de excluidos.",
        "The creator can't be on the excluded list.",
        "Der Ersteller kann nicht auf der Ausschlussliste stehen.");

    public static string BlacklistedNotMembers => Pick(
        "Los usuarios excluidos deben ser miembros del grupo.",
        "Excluded users must be members of the group.",
        "Ausgeschlossene Personen müssen Mitglieder der Gruppe sein.");

    public static string TargetNotMember => Pick(
        "El usuario objetivo debe ser miembro del grupo.",
        "The person being rated must be a member of the group.",
        "Die bewertete Person muss Mitglied der Gruppe sein.");

    public static string TeamMembersNotInGroup => Pick(
        "Todos los miembros de los equipos deben pertenecer al grupo.",
        "Every team member must belong to the group.",
        "Alle Teammitglieder müssen zur Gruppe gehören.");

    public static string PackNotFound => Pick(
        "El pack no existe.",
        "That pack doesn't exist.",
        "Dieses Pack gibt es nicht.");

    public static string LastEnabledPack => Pick(
        "No se puede desactivar: el grupo debe tener al menos un pack activo.",
        "Can't turn it off: the group needs at least one active pack.",
        "Deaktivieren nicht möglich: Die Gruppe braucht mindestens ein aktives Pack.");

    // ── Chat ────────────────────────────────────────────────────────────────────────────
    public static string NotAGroupMember => Pick(
        "No perteneces a este grupo.",
        "You're not a member of this group.",
        "Du bist kein Mitglied dieser Gruppe.");

    public static string NoActiveQuestionInGroup => Pick(
        "No hay pregunta activa en este grupo.",
        "There's no live question in this group.",
        "In dieser Gruppe gibt es keine aktive Frage.");

    public static string ChatLimitReached(int max) => Pick(
        $"Has alcanzado el límite de {max} mensajes por pregunta.",
        $"You've reached the limit of {max} messages per question.",
        $"Du hast das Limit von {max} Nachrichten pro Frage erreicht.");

    public static string ChatMessageEmpty => Pick(
        "El mensaje no puede estar vacío.",
        "The message can't be empty.",
        "Die Nachricht darf nicht leer sein.");

    public static string ChatMessageTooLong(int max) => Pick(
        $"El mensaje no puede superar {max} caracteres.",
        $"The message can't be longer than {max} characters.",
        $"Die Nachricht darf höchstens {max} Zeichen lang sein.");

    // ── Shop: streak freeze & repair (rama 35) ──────────────────────────────────────────
    public static string NotEnoughCoins => Pick(
        "No tienes monedas suficientes.",
        "You don't have enough coins.",
        "Du hast nicht genug Münzen.");

    public static string CosmeticAlreadyOwned => Pick(
        "Ya tienes eso.",
        "You already own that.",
        "Das hast du schon.");

    public static string CosmeticNotOwned => Pick(
        "Antes tienes que comprarlo en la tienda.",
        "You need to buy that in the shop first.",
        "Das musst du erst im Shop kaufen.");

    public static string NoItemOwned => Pick(
        "No te queda ninguno en tu inventario.",
        "You don't have any left in your inventory.",
        "Du hast keins mehr in deinem Inventar.");

    // ── Change my vote & nudge (rama 43) ────────────────────────────────────────────────
    public static string VoteChangeNotVoted => Pick(
        "Todavía no has votado esta pregunta.",
        "You haven't voted on this question yet.",
        "Du hast bei dieser Frage noch nicht abgestimmt.");

    public static string VoteAlreadyChanged => Pick(
        "Ya has cambiado tu voto en esta pregunta.",
        "You've already changed your vote on this question.",
        "Du hast deine Stimme bei dieser Frage schon geändert.");

    public static string FlashClosed => Pick(
        "Esta pregunta flash ya se ha cerrado.",
        "This flash question has already closed.",
        "Diese Blitzfrage ist schon geschlossen.");

    public static string NudgeInvalidTarget => Pick(
        "No puedes darle un toque a esa persona.",
        "You can't nudge that person.",
        "Diese Person kannst du nicht anstupsen.");

    public static string NudgeAlreadyVoted => Pick(
        "Ya ha votado esta pregunta.",
        "They've already voted on this question.",
        "Diese Person hat schon abgestimmt.");

    public static string NudgeUnreachable => Pick(
        "Ahora mismo no le llegan los toques.",
        "Nudges can't reach them right now.",
        "Anstupser erreichen diese Person gerade nicht.");

    public static string NudgeRecent => Pick(
        "Ya le han dado un toque en las últimas 24 horas.",
        "Someone already nudged them in the last 24 hours.",
        "Diese Person wurde in den letzten 24 Stunden schon angestupst.");

    public static string NudgeMessageTooLong(int max) => Pick(
        $"El mensaje no puede superar {max} caracteres.",
        $"The message can't be longer than {max} characters.",
        $"Die Nachricht darf höchstens {max} Zeichen lang sein.");

    public static string FreezeLimitReached => Pick(
        "Ya tienes el máximo de congelaciones en este grupo.",
        "You already hold the maximum number of freezes in this group.",
        "Du hast in dieser Gruppe schon die maximale Anzahl an Serienschutz.");

    public static string GiftFreezeLimitReached => Pick(
        "Ya tiene el máximo de congelaciones en este grupo.",
        "They already hold the maximum number of freezes in this group.",
        "Diese Person hat in dieser Gruppe schon die maximale Anzahl an Serienschutz.");

    public static string StreakNotRepairable => Pick(
        "Esta racha ya no se puede reparar.",
        "This streak can no longer be repaired.",
        "Diese Serie kann nicht mehr repariert werden.");

    public static string StreakRepairLimitReached => Pick(
        "Ya has reparado una racha este mes en este grupo.",
        "You've already repaired a streak in this group this month.",
        "Du hast in dieser Gruppe diesen Monat schon eine Serie repariert.");

    public static string CannotGiftYourself => Pick(
        "No puedes regalarte una congelación a ti mismo.",
        "You can't gift a freeze to yourself.",
        "Du kannst dir selbst keinen Serienschutz schenken.");

    public static string GiftTargetNotMember => Pick(
        "Esa persona no es miembro del grupo.",
        "That person isn't a member of the group.",
        "Diese Person ist kein Mitglied der Gruppe.");

    // ── Guess the author (rama 36) ──────────────────────────────────────────────────────
    public static string GuessNotAvailable => Pick(
        "En esta pregunta no se puede adivinar el autor.",
        "You can't guess the author of this question.",
        "Bei dieser Frage kannst du nicht raten, wer sie geschrieben hat.");

    public static string GuessMustVoteFirst => Pick(
        "Vota primero para adivinar quién la escribió.",
        "Vote first to guess who wrote it.",
        "Stimm zuerst ab, dann kannst du raten, wer sie geschrieben hat.");

    public static string AlreadyGuessed => Pick(
        "Ya has intentado adivinar el autor de esta pregunta.",
        "You've already had your guess at this question's author.",
        "Du hast bei dieser Frage schon geraten.");

    public static string GuessInvalidCandidate => Pick(
        "Esa persona no está entre los posibles autores.",
        "That person isn't one of the possible authors.",
        "Diese Person gehört nicht zu den möglichen Autor:innen.");

    public static string HintNotAvailable => Pick(
        "La pista no está disponible en esta pregunta.",
        "The hint isn't available for this question.",
        "Der Tipp ist bei dieser Frage nicht verfügbar.");

    // ── Race to select (rama 40) ────────────────────────────────────────────────────────
    public static string RaceLost => Pick(
        "Alguien se te ha adelantado: la pregunta ya está elegida.",
        "Someone beat you to it: the question has already been chosen.",
        "Da war jemand schneller: Die Frage ist schon ausgewählt.");

    // ── Flash question (rama 44) ────────────────────────────────────────────────────────
    public static string FlashGroupNotStarted => Pick(
        "El grupo necesita al menos 2 miembros para lanzar una pregunta flash.",
        "The group needs at least 2 members to launch a flash question.",
        "Die Gruppe braucht mindestens 2 Mitglieder für eine Blitzfrage.");

    public static string FlashDailyLimit => Pick(
        "Ya has lanzado una pregunta flash hoy en este grupo. Vuelve mañana.",
        "You've already launched a flash question in this group today. Come back tomorrow.",
        "Du hast heute in dieser Gruppe schon eine Blitzfrage gestartet. Versuch es morgen wieder.");

    public static string FlashNotFound => Pick(
        "Esa pregunta flash no existe.",
        "That flash question doesn't exist.",
        "Diese Blitzfrage gibt es nicht.");

    public static string MonthlySummaryNotFound => Pick(
        "No hay resumen de ese mes.",
        "There's no summary for that month.",
        "Für diesen Monat gibt es keine Zusammenfassung.");

    // ── Push notifications (recipient's language) ───────────────────────────────────────
    public static class Push
    {
        public static string SelectorTurnTitle(string lang, string groupName) => Pick(lang,
            $"🎯 ¡Te toca elegir en {groupName}!",
            $"🎯 Your turn to pick in {groupName}!",
            $"🎯 Du bist dran in {groupName}!");

        public static string SelectorTurnBody(string lang, string time) => Pick(lang,
            $"Eres el selector de hoy. Elige la pregunta antes de las {time}.",
            $"You're today's picker. Choose the question before {time}.",
            $"Heute wählst du die Frage aus – bis {time} Uhr.");

        public static string UserVotedBody(string lang, string voter) => Pick(lang,
            $"{voter} ha votado. ¿Cuál será su respuesta? 🗳️",
            $"{voter} has voted. What did they pick? 🗳️",
            $"{voter} hat abgestimmt. Was wohl die Antwort ist? 🗳️");

        public static string StreakDangerTitle(string lang) => Pick(lang,
            "🔥 ¡Tu racha está en peligro!",
            "🔥 Your streak is in danger!",
            "🔥 Deine Serie ist in Gefahr!");

        public static string StreakDangerBody(string lang, int streak, string groupName, string time) => Pick(lang,
            $"Tu racha de {streak} días en {groupName} termina a las {time}. ¡Vota antes!",
            $"Your {streak}-day streak in {groupName} ends at {time}. Vote before then!",
            $"Deine {streak}-Tage-Serie in {groupName} endet um {time} Uhr. Stimm vorher ab!");

        // Same reminder when a held freeze (§17.9) would cover the miss.
        public static string StreakDangerFrozenBody(string lang, int streak, string groupName, string time) => Pick(lang,
            $"Vota antes de las {time} en {groupName}. Si no, gastarás una congelación 🧊 para salvar tu racha de {streak} días.",
            $"Vote in {groupName} before {time}, or you'll spend a freeze 🧊 to save your {streak}-day streak.",
            $"Stimm in {groupName} vor {time} Uhr ab, sonst verbrauchst du einen Serienschutz 🧊, um deine {streak}-Tage-Serie zu retten.");

        // Flash question (§17.11): to everyone but the buyer, as soon as it is launched.
        public static string FlashQuestionTitle(string lang, string groupName) => Pick(lang,
            $"⚡ Pregunta flash en {groupName}",
            $"⚡ Flash question in {groupName}",
            $"⚡ Blitzfrage in {groupName}");

        public static string FlashQuestionBody(string lang, string launcher, string text) => Pick(lang,
            $"{launcher} pregunta: {text}",
            $"{launcher} asks: {text}",
            $"{launcher} fragt: {text}");

        // Toque / nudge (rama 43, §17.10), with the sender's optional message.
        public static string NudgeTitle(string lang, string groupName) => Pick(lang,
            $"👉 {groupName}",
            $"👉 {groupName}",
            $"👉 {groupName}");

        public static string NudgeBody(string lang, string sender) => Pick(lang,
            $"{sender} te recuerda que no has votado.",
            $"{sender} is reminding you that you haven't voted.",
            $"{sender} erinnert dich daran, dass du noch nicht abgestimmt hast.");

        public static string NudgeBodyWithMessage(string lang, string sender, string message) => Pick(lang,
            $"{sender} te recuerda que no has votado: «{message}»",
            $"{sender} is reminding you that you haven't voted: “{message}”",
            $"{sender} erinnert dich daran, dass du noch nicht abgestimmt hast: „{message}“");

        // Duel (§17.15 item 6): replaces the flash or new-question push for the two duelists.
        public static string DuelTitle(string lang, string groupName) => Pick(lang,
            $"⚔️ Duelo en {groupName}",
            $"⚔️ Duel in {groupName}",
            $"⚔️ Duell in {groupName}");

        // The chooser is the other side.
        public static string DuelChallengedBody(string lang, string chooser, string text) => Pick(lang,
            $"{chooser} te ha retado a un duelo: {text}",
            $"{chooser} has challenged you to a duel: {text}",
            $"{chooser} hat dich zum Duell herausgefordert: {text}");

        // The chooser put two other members to fight.
        public static string DuelPittedBody(string lang, string chooser, string text) => Pick(lang,
            $"{chooser} os ha puesto a pelear: {text}",
            $"{chooser} has pitted you against each other: {text}",
            $"{chooser} lässt euch gegeneinander antreten: {text}");

        // Race to select (§17.5): to everyone but the selector, 10 minutes before T.
        public static string RaceToSelectTitle(string lang, string groupName) => Pick(lang,
            $"⏱️ ¡Elige tú en {groupName}!",
            $"⏱️ Your pick in {groupName}!",
            $"⏱️ Du darfst wählen in {groupName}!");

        public static string RaceToSelectBody(string lang, int coins) => Pick(lang,
            $"Nadie ha elegido la próxima pregunta y sale en 10 minutos. Quien la elija primero gana {coins} 🪙.",
            $"Nobody has chosen the next question and it goes out in 10 minutes. First to choose wins {coins} 🪙.",
            $"Niemand hat die nächste Frage gewählt, und in 10 Minuten geht sie raus. Wer zuerst wählt, bekommt {coins} 🪙.");

        // Monthly summary (§17.12): every member, on the 1st at T.
        public static string MonthlySummaryTitle(string lang, string groupName) => Pick(lang,
            $"🎉 Resumen del mes en {groupName}",
            $"🎉 Your month in {groupName}",
            $"🎉 Monatsrückblick in {groupName}");

        public static string MonthlySummaryBody(string lang, int month, string groupName) => Pick(lang,
            $"Ya está el resumen de {MonthNames.Es[month - 1]} en {groupName}: premios, participación y tu match.",
            $"Your {MonthNames.En[month - 1]} summary for {groupName} is here: awards, participation and your match.",
            $"Der Rückblick auf den {MonthNames.De[month - 1]} in {groupName} ist da: Auszeichnungen, Beteiligung und dein Match.");
    }

    // Hardcoded so the container's globalization settings never change a push.
    private static class MonthNames
    {
        public static readonly string[] Es =
            ["enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"];
        public static readonly string[] En =
            ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
        public static readonly string[] De =
            ["Januar", "Februar", "März", "April", "Mai", "Juni", "Juli", "August", "September", "Oktober", "November", "Dezember"];
    }
}
