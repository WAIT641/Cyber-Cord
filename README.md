# Cyber-Cord

Jedná se o chatovací aplikaci, která lidem umožňuje
- Navázat mezi sebou přátelství a komunikovat spolu přes DMs (Direct Message)
- Vytvářet skupinové chaty, ve kterých mají všichni účastníci rovná oprávnění
- Vytvářet servery
    - Server spravuje vlastník
    - Server může mít více kanálů

Aplikace umožňuje přihlášení pomocí google účtu případně vytvoření vlastního účtu přímo v aplikaci (vyžaduje email).

# Endpointy

## Users Controller

- GET `/api/users`
    - Anonymní
    - Search na uživatele
        - Možnost získání více výsledků
        - Možnost získání jednoho výsledku
- GET `/api/users/{id}`
    - Anonymní
    - Získání základních informací o uživateli
- GET `/api/users/me`
    - Role User
    - Získá informace o aktuálním uživateli
- GET `/api/users/{id}/detail`
    - Role User
    - Musí se dotazovat vlastník účtu
    - Získání detailních informací o uživateli
- GET `/api/users/{id}/settings`
    - Role User
    - Musí se dotazovat vlastník účtu
    - Vrací nastavení účtu
- GET `/api/users/{id}/friends`
    - Role User
    - Musí se dotazovat vlastník účtu
    - Vrátí přátele uživatele
- GET `/api/users/{id}/pending`
    - Role User
    - Musí se dotazovat vlastník účtu
    - Vrátí nevyřešené žádosti o přátelství uživatele
- GET `/api/users/{id}/friends/{friendshipId}/chat`
    - Role User
    - Musí se dotazovat vlastník účtu, který je v daném přátelství
    - Vrátí přímý chat, který patří k danému přátelství
- POST `api/users`
    - Anonymní
    - Tvorba uživatele
- POST `api/users/{id}/activate`
    - Anonymní
    - Aktivace účtu
- POST `api/users/{id}/resendcode`
    - Anonymní
    - Znovuposlání aktivačního kódu
- POST `api/users/{id}/friends`
    - Role User
    - Musí se dotazovat vlastník účtu
    - Žádost o přátelství
- POST `api/users/{id}/ping`
    - Role User
    - Pošle ping danému uživateli
- POST `api/users/{id}/roles`
    - Role Admin
    - Přidá roli danému uživateli
- POST `api/users/{id}/pending/{friendshipId}/accept`
    - Role User
    - Musí se dotazovat vlastník účtu, který patří do daného přátelství
    - Přijmutí žádosti o přátelství
- PUT `api/users/{id}`
    - Role User
    - Musí se dotazovat vlastník účtu
    - Modifikace účtu
- PUT `api/users/{id}/password`
    - Role User
    - Musí se dotazovat vlastník účtu
    - Vyžaduje i původní heslo
    - Změna hesla
- PATCH `api/users/{id}/settings`
    - Role User
    - Musí se dotazovat vlastník účtu
    - Modifikace nastavení uživatele
- DELETE `api/users/{id}`
    - Role User
    - Musí se dotazovat vlastník účtu
    - Vyžaduje i heslo
    - Smaže účet uživatele
- DELETE `api/friends/{friendshipId}`
    - Role User
    - Musí se dotazovat vlastník účtu, který patří do daného přátelství
    - Smaže přátelství

## Chats Controller

- Vyžaduje roli User
- GET `api/chats`
    - Vyhledávání a získávání chatů, do kterých patří dotazující se uživatel
- GET `api/chats/{id}`
    - Uživatel musí být součástí chatu
    - Získání informací o daném chatu
- GET `api/chats/{id}/users`
    - Uživatel musí být součástí chatu
    - Získání uživatelů, kteří patří do daného chatu
- GET `api/chats/{id}/messages`
    - Uživatel musí být součástí chatu
    - Získávání zpráv v chatu
    - Využívá cursor pagination
- POST `api/chats`
    - Tvorba nového chatu
- POST `api/chats/{id}/users`
    - Uživatel musí být součástí chatu
    - Přidání uživatele do chatu
- POST `api/chats/{id}/messages`
    - Uživatel musí být součástí chatu
    - Poslání zprávy do chatu
- PUT `api/chats/{id}`
    - Uživatel musí být součástí chatu
    - Úprava chatu
- PUT - `api/chats/{id}/messages/{messageId}`
    - Uživatel musí být součástí chatu a musí vlastnit danou zprávu
    - Úprava dané zprávy
- DELETE `api/chats/{id}`
    - Uživatel musí být součástí chatu
    - Smazání chatu
- DELETE `api/chats/{id}/users/{userId}`
    - Uživatel musí být součástí chatu
    - Odstranění uživatele z chatu
- DELETE `api/chats/{id}/messages/{messageId}`
    - Uživatel musí být součástí chatu a zároveň musí být vlastníkem zprávy
    - Odstranění zprávy

## Servers Controller

- Vyžaduje roli User
- GET `api/servers`
    - Získá servery uživatele, který se dotazuje
- GET àpi/servers/{id}`
    - Uživatel musí být součástí serveru
    - Získá informace o daném serveru
- GET `api/servers/{id}/members`
    - Získání lidí na serveru
- GET `api/servers/{id}/bans`
    - Uživatel musí být součástí serveru
    - Získání zabanovaných lidí
- GET `api/servers/{id}/chanels`
    - Uživatle musí být součástí serveru
    - Získání kanálů na serveru
- GET `api/servers/{id}/chanels/{channelId}`
    - Uživatle musí být součástí serveru
    - Získání kanálu na serveru
- GET `api/servers/{id}/chanels/{channelId}/messages`
    - Uživatle musí být součástí serveru
    - Získání zpráv v kanálu
- GET `api/servers/{id}/chanels/{channelId}/messages/{messageId}`
    - Uživatle musí být součástí serveru
    - Získání zprávy v kanálu
- POST `api/servers`
    - Tvorba serveru
- POST `api/servers/{id}/members`
    - Uživatel musí být vlastníkem serveru
    - Přidání uživatele na server
- POST `api/servers/{id}/channels`
    - Uživatel musí být vlastníkem serveru
    - Tvorba kanálu na server
- POST `api/servers/{id}/owner/{newOwnerId}`
    - Uživatel musí být vlastníkem serveru
    - Převod vlastnictví serveru
- POST `api/servers/{id}/bans/{bannedUserId}`
    - Uživatel musí být vlastníkem serveru
    - Zabanování uživatele
- POST `api/servers/{id}/channels/{channelId}/messages`
    - Uživatel musí být součástí serveru
    . Poslání zprávy do kanálu
- PUT `api/servers/{id}`
    - Uživatel musí být vlastníkem serveru
    - Modifikace serveru
- PUT `api/servers/{id}/channels/{channelId}`
    - Uživatle musí být vlastníkem serveru
    - Úprava kanálu
- PUT `api/servers/{id}/chanels/{channelId}/messages/{messageId}`
    - Uživatle musí být součástí serveru a zároveň vlastnit danou zprávu
    - Úprava zprávy v kanálu
- DELETE `api/servers/{id}`
    - Uživatel musí být vlastníkem serveru
    - Smazání serveru
- DELETE `api/servers/{id}/members/{memberId}`
    - Uživatel musí být vlastníkem serveru
    - Odstranění uživatele ze serveru
- DELETE `api/servers/{id}/bans/{bannedUserId}`
    - Uživatel musí být vlastníkem serveru
    - Odbanování uživatele
- DELETE `api/servers/{id}/channels/{channelId}`
    - Uživatel musí být vlastníkem serveru
    - Smazání kanálu
- DELETE `api/servers/{id}/chanels/{channelId}/messages/{messageId}`
    - Uživatle musí být součástí serveru a zároveň vlastnit zprávu nebo být vlastníkem serveru
    - Smazání zprávy v kanálu

## Auth Controller

- POST `api/auth/login`
    - Anonymní
    - Vydání JWT tokenu
- POST `api/auth/logout`
    - Anonymní
    - Vymazání JWT tokenu
- POST `api/auth/ws-code`
    - Role User
    - Získání krátkodobého tokenu pro websocketu
- GET `api/auth/google-login`
    - Anonymní
    - Login na google
- GET `api/auth/google-callback`
    - Anonymní
    - Callback pro google OAuth

## Management Controller

- GET `api/management/latency`
    - Anonymní
    - Vypočítání latence
- GET `api/management/logs`
    - Role Admin
    - Filtrace a pagination logů