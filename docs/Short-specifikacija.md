# Short — specifikacija aplikacije

## 1. Svrha

**Short** je mala Windows aplikacija koja duboku putanju do foldera predstavlja kao virtuelni disk pomoću komande `SUBST`.

Cilj je da korisnik iz Windows Explorera, preko kontekstnog menija desnog dugmeta miša, može da:

- napravi virtuelni disk za izabrani folder;
- dobije automatski izabrano slobodno slovo diska;
- nastavi da koristi taj disk i nakon ponovnog pokretanja računara;
- ukloni mapiranje opcijom **Unshort**, ali samo ako je disk kreirala aplikacija Short.

Aplikacija ne premešta, ne kopira i ne menja postojeće fajlove. Ona samo mapira folder na slovo diska. Sve redovne korisničke radnje obavljaju se kroz kontekstni meni Windows Explorera; nije predviđen zaseban glavni prozor aplikacije.

## 2. Osnovni korisnički tokovi

### Osnovni princip interakcije

- Sve funkcije dostupne korisniku pokreću se desnim klikom u Windows Exploreru.
- Na folderu je dostupna opcija `Short → Create drive`.
- Na disku koji je napravio Short dostupna je opcija `Unshort`.
- Ne predviđa se zaseban GUI za svakodnevni rad. Tehnički proces koji radi u pozadini može biti nevidljiv korisniku.
- Instalacija i deinstalacija su standardne instalacione radnje; deinstalacija mora očistiti mapiranja i Explorer integraciju koju je instalirao Short.


### 2.1. Kreiranje diska iz foldera

1. Korisnik u Exploreru klikne desnim dugmetom na folder.
2. U kontekstnom meniju izabere **Short → Create drive**.
3. Short proverava koja su slova diskova slobodna.
4. Aplikacija bira prvo odgovarajuće slobodno slovo, prema definisanom redosledu.
5. Short kreira `SUBST` mapiranje za izabrani folder.
6. Novi disk se pojavljuje u **This PC**.
7. Short pamti mapiranje kako bi mogao da ga obnovi nakon prijave korisnika u Windows.

Ako nema slobodnog slova diska, aplikacija prikazuje jasnu poruku i ne menja postojeća mapiranja.

Posebni slučajevi:

- Ako je izabrani folder već mapiran, Short ne pravi novo mapiranje, već prijavljuje postojeće slovo i otvara ga.
- Ako je folder izabran kroz Short disk (npr. `R:\Ugovori\2024`), Short putanju prevodi na stvarnu i mapira novo slovo direktno na nju. Novi disk nema nikakvu vezu sa `R:`; Short mapiranja nikada ne zavise jedno od drugog.
- Podfolder već mapiranog foldera, izabran preko originalne putanje, dobija sopstveno nezavisno mapiranje.

### 2.2. Uklanjanje diska iz This PC

1. Korisnik klikne desnim dugmetom na disk koji je napravio Short.
2. Izabere **Unshort**.
3. Short proverava da li je disk evidentiran kao njegovo mapiranje.
4. Aplikacija dodatno proverava da li se trenutna ciljna putanja poklapa sa sačuvanom putanjom.
5. Samo ako su provere uspešne, Short uklanja mapiranje i briše odgovarajući zapis iz svoje evidencije.

Ako disk nije napravio Short ili se njegova ciljna putanja ne poklapa sa evidencijom, opcija **Unshort** ne sme da ukloni disk. Aplikacija treba da prikaže poruku da mapiranje nije prepoznato kao Short mapiranje.

## 3. Funkcionalni zahtevi

### FR-01 — Kontekstni meni foldera

- Dodati opciju `Short → Create drive` za foldere u Windows Exploreru.
- Proslediti aplikaciji tačnu putanju izabranog foldera.
- Ne nuditi kreiranje mapiranja za fajlove.
- Ako je izabrani folder već mapiran, ne kreirati novo mapiranje; prijaviti postojeće slovo.
- Ako je folder izabran preko Short diska, mapiranje kreirati na stvarnu ciljnu putanju, nikada na drugi Short disk. Short mapiranja ne smeju međusobno zavisiti.
- Ne mapirati koren diska.

### FR-02 — Automatski izbor slova

- Pronaći slobodno slovo diska.
- Proveriti zauzetost slova pre kreiranja mapiranja.
- Ne prepisivati postojeći disk, mrežno mapiranje ili drugo `SUBST` mapiranje.
- Ako se slovo zauzme između provere i kreiranja, ponoviti izbor sa sledećim slobodnim slovom.

### FR-03 — Trajnost nakon restarta

- `SUBST` mapiranje samo po sebi ne treba smatrati trajnim.
- Short mora sačuvati evidenciju svojih mapiranja i ponovo ih uspostaviti pri prijavi korisnika u Windows.
- Pri obnovi proveriti da li ciljna putanja i dalje postoji i da li je slovo slobodno.
- Ako obnova nije moguća, ne prepisivati postojeće mapiranje; prijaviti problem korisniku.
- Ako je ciljna putanja nedostupna, zadržati zapis i omogućiti ponovni pokušaj.
- Pri prijavi Short automatski ponavlja obnovu nedostupnih putanja do 30 sekundi (mreža i sync klijent možda još nisu spremni). Ako problem ostane, prikazuje jednu poruku sa listom problema i dugmićima **Retry / Cancel**.

### FR-04 — Kontekstni meni diska

- Dodati opciju `Unshort` za diskove koje je kreirao Short.
- Ne nuditi niti izvršavati uklanjanje proizvoljnih diskova.
- Uklanjanje mora proveriti evidenciju i stvarno trenutno mapiranje.
- Uklanjanje virtuelnog diska ne sme brisati sadržaj ciljnog foldera.
- Prikaz: stavka `Unshort` se registruje sa uslovom `AppliesTo` koji sadrži samo slova aktivnih Short diskova; Short ga ažurira pri svakom Create, Unshort i obnovi. Kada nema aktivnih Short diskova, stavka ne postoji.
- Izvršenje: bez obzira na prikaz, `Unshort` pri izvršenju uvek proverava evidenciju i stvarno mapiranje. Uklanjanje se radi sa `DDD_EXACT_MATCH_ON_REMOVE`, pa Windows sam odbija uklanjanje ako slovo pokazuje na nešto drugo.

### FR-05 — Evidencija i bezbednost

- Čuvati podatke samo o mapiranjima kojima upravlja Short.
- Ne zaključivati da je disk u vlasništvu Short-a samo na osnovu slova diska.
- Ako se evidencija izgubi ili se mapiranje promeni spolja, postupati bezbedno: ne uklanjati mapiranje automatski.
- Prikazati razumljive poruke za greške i konflikte.

## 4. Arhitektura

Aplikacija treba da bude mala i modularna, bez nepotrebnih servisa i složene infrastrukture.

### 4.1. Shell Integration

Odgovorna za kontekstni meni Windows Explorera:

- `Short → Create drive` za foldere;
- `Unshort` za prepoznate Short diskove u prikazu This PC.

Integracija mora biti testirana na ciljnoj verziji Windowsa 11.

**Odluka za v1:** klasični registry verbovi u `HKCU\Software\Classes` (`Directory\shell` i `Drive\shell`) koji pozivaju `Short.exe`. Bez COM-a, bez admin prava, per-user. Na Windows 11 stavke se nalaze u meniju **Show more options**. Prikaz direktno u novom Windows 11 meniju zahteva `IExplorerCommand`, sparse MSIX paket i potpisivanje i nije deo v1.

### 4.2. Mapping Manager

Centralni deo aplikacije koji:

- bira slobodno slovo;
- kreira `SUBST` mapiranje;
- proverava postojeća mapiranja;
- uklanja samo potvrđena Short mapiranja;
- obnavlja mapiranja pri prijavi korisnika.

### 4.3. Persistence

Čuva evidenciju mapiranja između prijava i restartovanja računara.

Predložena lokacija:

`%LOCALAPPDATA%\Short\mappings.json`

Za svako mapiranje čuvati najmanje:

- slovo diska;
- punu ciljnu putanju;
- datum kreiranja;
- verziju zapisa, radi budućih promena formata.

Evidencija je vlasnička evidencija aplikacije, a ne dokaz koji se prihvata bez provere trenutnog stanja sistema.

### 4.4. Startup / Logon

Mali mehanizam za pokretanje Short-a pri prijavi korisnika i obnovu evidentiranih mapiranja.

**Short radi isključivo po korisniku i bez administratorskih prava.** Instalira se po korisniku, sva mapiranja kreira u korisnikovoj sesiji i nikada ne zahteva niti koristi elevaciju. Mapiranja su vidljiva samo tom korisniku i samo neeleviranim procesima, uključujući Explorer.

Obnova pri prijavi: vrednost `Short` u `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`. Nema Windows servisa.

Razlog: sa uključenim UAC-om eleviran proces ima drugi token i drugu logon sesiju, pa i drugi DosDevices prostor. Mapiranje napravljeno iz eleviranog procesa Explorer ne vidi, a eleviran proces ne vidi (i ne može da ukloni) korisnikova mapiranja. Ako se Short ipak pokrene elevirano, odbija rad sa porukom.

## 5. Predloženi model podataka

Jedan zapis mapiranja:

| Polje | Namena |
|---|---|
| `drive_letter` | Slovo koje je Short dodelio, npr. `R:` |
| `target_path` | Apsolutna putanja foldera |
| `created_at` | Vreme kreiranja zapisa |
| `schema_version` | Verzija formata evidencije |

Primer sadržaja je konceptualan; format i validacija određuju se pri implementaciji. Ne čuvati nepotrebne podatke o korisniku.

## 6. Pravila za izbor slova

- Definisati deterministički redosled izbora slova, na primer od `R:` ka `Z:`, pa po potrebi proširiti opseg.
- Proveriti i lokalne diskove, mrežna mapiranja i postojeća `SUBST` mapiranja.
- Slovo koje je već evidentirano u Short-u nije automatski slobodno: proveriti i stvarno stanje Windowsa.
- Ne menjati postojeće mapiranje da bi se oslobodilo slovo.
- Redosled u v1: `R:`–`Z:`, zatim `Q:` ka `D:` (što dalje od slova koja Windows dodeljuje USB uređajima).
- Kao zauzeta tretirati i slova trajnih mrežnih diskova (`HKCU\Network\<slovo>`), jer se oni povezuju pri prijavi, moguće tek posle Short-a.
- Kao zauzeta tretirati i slova iz Short evidencije koja trenutno nisu aktivna (rezervisana za taj zapis).
- `DefineDosDevice` ne odbija zauzeto ime, već ga zasenčuje. Zato se posle kreiranja proverava da je naša definicija jedina; ako nije, Short vraća prethodno stanje i pokušava sledeće slovo.
- Poznato ograničenje: USB disk koji kasnije dobije isto globalno slovo biće zasenčen Short diskom za tog korisnika. Short to ne može pouzdano da spreči.

## 7. Ograničenja i tehničke napomene

- `SUBST` skraćuje vidljivu putanju, ali ne garantuje da će svaka Explorer operacija podržati maksimalnu dužinu putanje.
- Mapiranja mogu biti vezana za korisničku prijavu i kontekst procesa. Zato kreiranje i obnavljanje treba obavljati u istom korisničkom kontekstu u kojem se koristi Explorer.
- Ako se Short pokrene sa drugim nivoom privilegija ili u drugoj sesiji, disk možda neće biti vidljiv očekivanom Explorer procesu.
- Promena ciljnog foldera, uklanjanje foldera ili zauzimanje slova od strane drugog uređaja mora biti obrađeno bez gubitka ili prepisivanja podataka.
- Short ne treba da menja NTFS dozvole niti da zaobilazi prava pristupa.
- Mapiranja se prave direktno preko `DefineDosDevice` sa `DDD_RAW_TARGET_PATH` i NT putanjom (`\??\C:\...`, `\??\UNC\server\share\...`), ne pozivanjem `subst.exe`. Za ciljnu putanju tada ne važi MAX_PATH, pa se mogu mapirati i folderi čija je stvarna putanja već duža od 260 znakova.
- Granica od 260 znakova važi za putanju koju aplikacija prosledi Win32 API-ju. Preko Short diska budžet počinje od `R:\`. Problem se vraća ako korisnik isti fajl otvori preko originalne putanje (Recent, Office MRU, link) ili ako aplikacija sama razreši stvarnu putanju.
- Rizik: brisanje fajlova preko SUBST diska na nekim verzijama Windowsa zaobilazi Recycle Bin. Mora se eksplicitno testirati i dokumentovati korisnicima.

## 8. Deinstalacija (obavezno)

Deinstalacija mora da ukloni sve tragove aplikacije Short i sva aktivna `SUBST` mapiranja koja je Short napravio.

1. Pronaći sva mapiranja evidentirana u Short-ovoj evidenciji.
2. Za svako mapiranje proveriti da li trenutno slovo zaista pokazuje na očekivanu ciljnu putanju.
3. Ukloniti samo potvrđena Short mapiranja. Ne uklanjati mapiranja koja su u međuvremenu promenjena ili ne mogu pouzdano da se identifikuju; prikazati izveštaj o takvim izuzecima umesto da se dira tuđe mapiranje.
4. Ukloniti sve Short stavke iz kontekstnih menija Explorera, uključujući `Short → Create drive` i `Unshort`.
5. Ukloniti mehanizam pokretanja pri prijavi korisnika, aplikacione fajlove i evidenciju Short-a, nakon obrade mapiranja.
6. Ne brisati ciljne foldere, njihove fajlove, niti bilo kakve korisničke podatke koji nisu evidencija aplikacije.
7. Ako je neki Short disk trenutno nedostupan ili se ne može bezbedno ukloniti, deinstalacija mora jasno prijaviti šta je ostalo i zašto. Ne sme tvrditi da je potpuno čišćenje uspešno ako postoje preostala mapiranja ili stavke menija.

Deinstalacija se pokreće iz Apps & Features (per-user unos u `HKCU\...\Uninstall\Short`) i radi u korisnikovom neeleviranom kontekstu, jer samo tu vidi i može da ukloni korisnikova mapiranja. Svaki korisnik na mašini ima svoju instalaciju.

**Kriterijum uspeha deinstalacije:** nakon uspešne deinstalacije ne postoji nijedno aktivno mapiranje koje je Short napravio, nijedna Short stavka u kontekstnom meniju i nijedan mehanizam koji bi ponovo kreirao mapiranja pri sledećoj prijavi.

## 9. Kriterijumi prihvatanja

Aplikacija se smatra spremnom tek kada su proverene sledeće stavke:

- [ ] Sve redovne korisničke funkcije dostupne su kroz desni klik; nije potreban zaseban glavni prozor.
- [ ] Desni klik na folder nudi `Short → Create drive`.
- [ ] Short bira slobodno slovo i kreira mapiranje ka tačnom folderu.
- [ ] Disk se pojavljuje u This PC i otvara odgovarajući folder.
- [ ] Mapiranje se vraća nakon odjave/prijave i nakon restarta računara.
- [ ] Desni klik na Short disk nudi `Unshort`.
- [ ] `Unshort` uklanja samo mapiranje koje je Short evidentirao i čija se ciljna putanja poklapa.
- [ ] Pokušaj uklanjanja običnog diska, mrežnog diska ili tuđeg `SUBST` mapiranja ne menja stanje sistema.
- [ ] Uklanjanje mapiranja ne briše niti menja sadržaj foldera.
- [ ] Konflikt slova i nedostupna ciljna putanja obrađuju se bez prepisivanja postojećeg stanja.
- [ ] Deinstalacija uklanja sva potvrđena Short `SUBST` mapiranja, Short stavke iz kontekstnog menija i automatsko pokretanje.
- [ ] Deinstalacija ne uklanja tuđa ili izmenjena mapiranja i ne briše sadržaj ciljnih foldera.
- [ ] Ako čišćenje nije potpuno, deinstalacija prijavljuje konkretne preostale stavke i ne prikazuje lažnu potvrdu potpunog uspeha.
- [ ] Kreiranje, otvaranje, kopiranje, preimenovanje i brisanje fajlova testirani su na putanjama koje su ranije izazivale problem.
- [ ] Ponovljeni Short na istom folderu ne pravi novo slovo, već prijavljuje postojeće.
- [ ] Short na folderu izabranom kroz Short disk pravi nezavisno mapiranje na stvarnu putanju; Unshort jednog ne utiče na drugi.
- [ ] Ponašanje Recycle Bin-a pri brisanju preko Short diska je provereno i dokumentovano.
- [ ] Office fajlovi u OneDrive/SharePoint sync folderu otvaraju se i čuvaju preko Short diska (uključujući Files On-Demand fajlove koji nisu preuzeti).
- [ ] Short pokrenut sa administratorskim pravima odbija rad.

## 10. Šta nije cilj

- Nije zamena za particionisanje diska.
- Ne premešta i ne duplira podatke.
- Ne menja fizičku strukturu direktorijuma.
- Ne obećava da će ukloniti sva ograničenja Windows putanja.
- Ne upravlja diskovima koje nije kreirao Short.

## 11. Sažetak

**Short** je Windows Explorer dodatak bez zasebnog glavnog prozora: sve redovne operacije rade se desnim klikom. Aplikacija kreira `SUBST` mapiranje, automatski bira slobodno slovo, obnavlja mapiranja pri prijavi i omogućava uklanjanje preko opcije `Unshort`. Deinstalacija mora ukloniti sva potvrđena Short mapiranja, sve Short stavke kontekstnog menija i automatsko pokretanje. Ključni uslov je da Short nikada ne uklanja mapiranje koje ne može pouzdano da identifikuje kao svoje.
