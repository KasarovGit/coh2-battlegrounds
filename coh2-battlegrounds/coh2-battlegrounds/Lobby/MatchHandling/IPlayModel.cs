﻿using Battlegrounds.Modding;
using Battlegrounds.Networking.Communication.Connections;

namespace BattlegroundsApp.Lobby.MatchHandling;

delegate void PrepareCancelHandler(object model);

delegate void PrepareOverHandler(IPlayModel model);

delegate void PlayOverHandler(IPlayModel model);

delegate void GameStartupHandler();

internal interface IPlayModel {

    UploadProgressCallbackHandler? UploadProgressCallback { get; set; }

    void Prepare(ModPackage modPackage, PrepareOverHandler prepareOver, PrepareCancelHandler prepareCancel);

    void Play(GameStartupHandler? startupHandler, PlayOverHandler matchOver);

}
