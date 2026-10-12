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

    public static string GiftTargetNotMember => Pick(
        "Esa persona no es miembro del grupo.",
        "That person isn't a member of the group.",
        "Diese Person ist kein Mitglied der Gruppe.");

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
    }
}
