# Changelog

## v1.2.1 (2026-06-20) - Career deck-construction averages

- Changed English career title to career stats.
- Added deck-construction averages in career view:
  - Average run time
  - Average deck size
  - Average relic count
  - Average card-type counts (Attack / Skill / Power)
  - Average rarity counts (Common / Uncommon / Rare)
- Added persistence mapping for the new career-average fields in CareerStatsCache.
- Added stale-cache guard for older snapshots without the new averages.

## v1.2.0 (2026-06-20) 鈥?v0.107.1 姝ｅ紡鐗堥€傞厤 + 瀹屽叏鏈湴鍖?
### v0.107.1 姝ｅ紡鐗?API 閫傞厤

- `RunManager.SetUpNewSinglePlayer/SetUpNewMultiPlayer` 
  鈫?`SetUpNewSingleplayer/SetUpNewMultiplayer`锛?  `SetUpSavedSinglePlayer/SetUpSavedMultiPlayer` 
  鈫?`SetUpSavedSingleplayer/SetUpSavedMultiplayer`銆?- `TemporaryStrengthPower.AfterTurnEnd` 
  鈫?`TemporaryStrengthPower.AfterSideTurnEnd`锛屼繚鎸佷复鏃跺姏閲忓洖婊氬綊鍥犻€昏緫涓嶅彉銆?- `MerchantRoom.Inventory` 绉婚櫎 鈫?浣跨敤 `GetLocalInventory()` 璁块棶鏈湴鍟嗗簵搴撳瓨锛?  缁х画璁板綍鏈湴鍗＄墝涓婃灦鏁版嵁鐢ㄤ簬鍟嗗簵缁熻銆?- `STS2_GE_V105` 鐜颁綔涓洪粯璁ょ紪璇戝父閲忥紝鐩爣鐗堟湰閿佸畾涓?`v0.107.1`銆?
### 鏈湴鐗堣兘鍔涗繚鎸侊紙涓?v1.1.0 涓€鑷达級

- 鎴樻枟璐＄尞杩借釜涓?F8 璐＄尞闈㈡澘锛堝崱鐗?閬楃墿/鑽按/鐘舵€佸叏閲忓綊鍥狅級銆?- 淇濆瓨/璇绘。鍚庣殑鎴樻枟璐＄尞涓庢暣灞€姹囨€绘寔涔呭寲銆?- 涓汉鐢熸动缁熻涓?Run History 鍗曞眬璇︾粏瑙嗗浘銆?- 椤舵爮鑽按鎺夎惤 / 鍗＄墝鎺夎惤姒傜巼鎸囩ず鍣ㄣ€?- 鍦板浘闂彿鎴块伃閬囨鐜囥€佸晢搴椾环鏍奸鏈熼潰鏉裤€?- 鎬墿鎰忓浘鐘舵€佹満闈㈡澘涓庣浉鍏宠緟鍔╂彁绀恒€?- F7 璁剧疆闈㈡澘锛氳瑷€鍒囨崲 + 鍔熻兘寮€鍏筹紙鍏ㄩ儴鏈湴锛夈€?
### 瀹屽叏鏀惧純鑱旂綉涓庣ぞ鍖虹粺璁″姛鑳斤紙Local Edition 鍩虹嚎锛?
- 鍒犻櫎鑷姩鏇存柊瀹㈡埛绔細绉婚櫎 `Api/Updater.cs`锛屼笉鍐嶅湪鍚姩鏃跺鎺?  杩滅▼鍏冩暟鎹垨涓嬭浇 DLL銆?- 鍒犻櫎绂荤嚎涓婁紶闃熷垪锛氱Щ闄?`Util/OfflineQueue.cs` 涓庣浉鍏抽厤缃」
  锛坄PendingDir`銆佺绾块槦鍒椾笂闄愮瓑锛夈€?- 鍒犻櫎涓婁紶鐩稿叧 UI锛氱Щ闄?`HistoryImportDialog`銆乣ImportProgressLabel`銆?  `UploadNotice`锛屼笉鍐嶅脊鍑衡€滄槸鍚︿笂浼犲巻鍙插灞€鈥濇垨鏄剧ず涓婁紶杩涘害/鎻愮ず銆?- 閰嶇疆绮剧畝锛歚ModConfig` 鍘婚櫎 `ApiBaseUrl`銆乣QueryTimeoutMs`銆?  `UploadTimeoutMs`銆乣AllowHttp`銆乣AutoUpdate`銆乣EnableUpload`銆?  `HistoryImportCompleted` 绛夊瓧娈碉紝浠呬繚鐣欐湰鍦拌缃拰缂撳瓨璺緞銆?- 鏂囨绮剧畝锛歚Localization` 鍒犻櫎涓婁紶/绀惧尯瀵规瘮鐩稿叧 key锛屽
  `settings.upload`銆乣settings.auto_update`銆乣career.community_comparison`銆?  `card_lib.community`銆佸悇绫?upload/import 鏂囨锛岄伩鍏?UI 涓嚭鐜颁换浣?  鈥滀笂浼?绀惧尯鏈嶅姟鍣ㄢ€濇彁绀恒€?- 鍏ュ彛閫昏緫锛歚CommunityStatsMod.Initialize` 涓嶅啀璁剧疆 `Updater.Edition`
  鎴栬皟鐢?`CheckForUpdateAsync`锛屽畬鍏ㄨ劚绂昏繙绔湇鍔°€?
> 娉細濡傞渶閲嶆柊寮曞叆绀惧尯缁熻锛屽皢浠?`sts2_community_stats` 涓轰富绾匡紝
> 鏈湴鐗堢户缁繚鎸佲€滅函绂荤嚎淇℃伅澧炲己鈥濈殑瀹氫綅銆?
---

## v1.1.0 (2026-05-08) 鈥?v0.105.0 Beta 鍏煎 + 鏂板姛鑳?
### v0.105.0 Beta 閫傞厤

- `CombatState` 鈫?`ICombatState`锛歚#if STS2_GE_V105` 缂栬瘧鏈熷畯锛宍dotnet build -p:DefineConstants=STS2_GE_V105`
- `ShowsInfiniteHp` 鈫?`HpDisplay.IsInfinite()`锛沗AfterCardGeneratedForCombat` bool鈫扨layer?

### 鏂板姛鑳斤細涓汉鐢熸动缁熻"鏈€杩戝灞€"绛涢€?

- OptionButton锛堝叏閮?鏈€杩慛灞€锛? SpinBox锛屾寜 `StartTime` 闄嶅簭 `Take(N)`

### Bug 淇

- Infused Core orb 鍊间慨楗板櫒杩借釜锛堝垪琛ㄦ浛浠ｅ崟渚嬶級
- 鑽嗘/鐏劙灞忛殰澶氭簮褰掑洜
- Sword Sage Replay 1 鍚?FORGE:BASE 淇
- F7 鍔熻兘寮€鍏虫寔涔呭寲

---

## v1.0.1 (2026-05-06) 鈥?Bug 淇

### 鎬墿鎰忓浘鐘舵€佹満闈㈡澘娈嬬暀淇

- 鎬墿姝讳骸鏃惰嫢榧犳爣浠嶆偓鍋滐紝`OnUnfocus` 涓嶈Е鍙?鈫?闈㈡澘姘镐箙娈嬬暀銆?
- `IntentHoverPatch` 鏂板 `ForceHideAll()`锛屾垬鏂楀紑濮?缁撴潫鏃剁Щ闄ゆ墍鏈?intent 闈㈡澘銆?
- `ShowPanel` 澧炲姞 `IsInsideTree()` + `IsDead` 妫€鏌ャ€?
- `ForceHideAll` 鏀逛负鎸?metadata 鏍囪鏌ユ壘闈㈡澘锛堜慨澶?`InfoModPanel.Create()` 鍥哄畾鍚嶇О瀵艰嚧鏌ユ壘澶辫触锛夈€?

### save&load 鍚?avg Damage per turn 鏁版嵁澶辩湡淇

- `RunContributionAggregator._encounters` 鏈寔涔呭寲 鈫?save&load 鍚?`TotalRunTurns = 0`銆?
- `LiveContributionSnapshot` 鏂板 `Encounters` 瀛楁锛屽畬鏁翠繚瀛?鎭㈠ encounter 璁板綍銆?

---

## v1.0.0 (2026-05-05) 鈥?Local Edition Initial Release

Forked from `sts2_community_stats` v0.14.0. All server-dependent features removed; only local functionality retained.

### Removed (vs. community edition)
- All server communication (API client, stats provider, cache, offline queue, history import)
- Community stat labels on card rewards, shop items, relic tooltips, event options
- Map encounter danger overlay
- Data upload / download
- Filter panel community data controls (ascension range, version, branch, win rate, character filter)
- "My data" vs "Community" dual-column comparison

### Changed
- **F9 鈫?F7**: Settings panel hotkey
- **Settings panel**: simplified to language selector + feature toggles only
- **Compendium card library / relic collection**: show personal data only (single column, from local RunHistory)
- Mod ID: `sts2_community_stats` 鈫?`sts2_statsthespire_local`

### Retained (all local features)
- Combat contribution tracking and panel (F8)
- Real-time contribution updates during combat
- Mid-run save/quit contribution persistence
- Personal career statistics (Compendium 鈫?Character Data)
- Run history single-run stats and contribution replay
- Shop price panel
- Card drop odds indicator
- Potion drop odds indicator
- Unknown room encounter probability
- Monster intent state machine
- Feature toggles
- Language switching (涓枃 / English)

