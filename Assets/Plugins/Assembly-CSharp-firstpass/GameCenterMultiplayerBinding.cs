#if UNITY_IOS
using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class GameCenterMultiplayerBinding : MonoBehaviour
{
	[DllImport("__Internal")]
	private static extern void _gameCenterMultiplayerSetShouldReinvitePlayerOnDisconnect(bool shouldReinvite);

	public static void setShouldReinvitePlayerOnDisconnect(bool shouldReinvite)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			_gameCenterMultiplayerSetShouldReinvitePlayerOnDisconnect(shouldReinvite);
		}
	}

	[DllImport("__Internal")]
	private static extern void _gameCenterMultiplayerShowMatchmakerWithMinMaxPlayers(int minPlayers, int maxPlayers);

	public static void showMatchmakerWithMinMaxPlayers(int minPlayers, int maxPlayers)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			_gameCenterMultiplayerShowMatchmakerWithMinMaxPlayers(minPlayers, maxPlayers);
		}
	}

	[DllImport("__Internal")]
	private static extern void _gameCenterMultiplayerShowMatchmakerWithFilters(int minPlayers, int maxPlayers, uint playerGroup, uint playerAttributes, string playersToInvite);

	public static void showMatchmakerWithFilters(int minPlayers, int maxPlayers, uint playerGroup, uint playerAttributes, string[] playersToInvite)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			string playersToInvite2 = ((playersToInvite == null) ? null : string.Join(",", playersToInvite));
			_gameCenterMultiplayerShowMatchmakerWithFilters(minPlayers, maxPlayers, playerGroup, playerAttributes, playersToInvite2);
		}
	}

	[DllImport("__Internal")]
	private static extern void _gameCenterMultiplayerShowMatchmakerWithPlayersToInvite(string participants);

	public static void showMatchmakerWithPlayersToInvite(string[] participants)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			_gameCenterMultiplayerShowMatchmakerWithPlayersToInvite(string.Join(",", participants));
		}
	}

	[DllImport("__Internal")]
	private static extern void _gameCenterMultiplayerAddPlayersToCurrentMatchWithAppleInterface();

	public static void addPlayersToCurrentMatchWithAppleInterface()
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			_gameCenterMultiplayerAddPlayersToCurrentMatchWithAppleInterface();
		}
	}

	[DllImport("__Internal")]
	private static extern int _gameCenterMultiplayerExpectedPlayerCount();

	public static int expectedPlayerCount()
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			return _gameCenterMultiplayerExpectedPlayerCount();
		}
		return -1;
	}

	[DllImport("__Internal")]
	private static extern void _gameCenterMultiplayerShowFriendRequestController();

	public static void showFriendRequestController()
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			_gameCenterMultiplayerShowFriendRequestController();
		}
	}

	[DllImport("__Internal")]
	private static extern void _gameCenterMultiplayerShowFriendRequestControllerWithOptions(string message, string participants);

	public static void showFriendRequestController(string message, string[] recipients)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			_gameCenterMultiplayerShowFriendRequestControllerWithOptions(message, string.Join(",", recipients));
		}
	}

	[DllImport("__Internal")]
	private static extern string _gameCenterMultiplayerSendMessageToAllPeers(string gameObject, string method, string param, bool reliably);

	public static string sendMessageToAllPeers(string gameObject, string method, string param, bool reliably)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			return _gameCenterMultiplayerSendMessageToAllPeers(gameObject, method, param, reliably);
		}
		return string.Empty;
	}

	[DllImport("__Internal")]
	private static extern string _gameCenterMultiplayerSendMessageToPeers(string playerIds, string gameObject, string method, string param, bool reliably);

	public static string sendMessageToPeers(string[] playerIds, string gameObject, string method, string param, bool reliably)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			return _gameCenterMultiplayerSendMessageToPeers(string.Join(",", playerIds), gameObject, method, param, reliably);
		}
		return string.Empty;
	}

	[DllImport("__Internal")]
	private static extern string _gameCenterMultiplayerSendRawMessageToAllPeers(string gameObject, string method, byte[] param, int length, bool reliably);

	public static string sendRawMessageToAllPeers(string gameObject, string method, byte[] param, bool reliably)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			return _gameCenterMultiplayerSendRawMessageToAllPeers(gameObject, method, param, param.Length, reliably);
		}
		return string.Empty;
	}

	[DllImport("__Internal")]
	private static extern string _gameCenterMultiplayerSendRawMessageToPeers(string playerIds, string gameObject, string method, byte[] param, int length, bool reliably);

	public static string sendRawMessageToPeers(string[] playerIds, string gameObject, string method, byte[] param, bool reliably)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			return _gameCenterMultiplayerSendRawMessageToPeers(string.Join(",", playerIds), gameObject, method, param, param.Length, reliably);
		}
		return string.Empty;
	}

	[DllImport("__Internal")]
	private static extern string _gameCenterGetAllConnectedPlayerIds();

	public static string[] getAllConnectedPlayerIds()
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			string text = _gameCenterGetAllConnectedPlayerIds();
			return text.Split(new string[1] { "," }, StringSplitOptions.None);
		}
		return new string[0];
	}

	[DllImport("__Internal")]
	private static extern void _gameCenterMultiplayerDisconnectFromMatch();

	public static void disconnectFromMatch()
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			_gameCenterMultiplayerDisconnectFromMatch();
		}
	}

	[DllImport("__Internal")]
	private static extern void _gameCenterChooseBestHostPlayer();

	public static void chooseBestHostPlayer()
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			_gameCenterChooseBestHostPlayer();
		}
	}

	[DllImport("__Internal")]
	private static extern bool _gameCenterMultiplayerIsVOIPAllowed();

	public static bool isVOIPAllowed()
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			return _gameCenterMultiplayerIsVOIPAllowed();
		}
		return false;
	}

	[DllImport("__Internal")]
	private static extern void _gameCenterMultiplayerEnableVoiceChat(bool isEnabled);

	public static void enableVoiceChat(bool isEnabled)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			_gameCenterMultiplayerEnableVoiceChat(isEnabled);
		}
	}

	[DllImport("__Internal")]
	private static extern void _gameCenterMultiplayerCloseAllOpenVoiceChats();

	public static void closeAllOpenVoiceChats()
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			_gameCenterMultiplayerCloseAllOpenVoiceChats();
		}
	}

	[DllImport("__Internal")]
	private static extern void _gameCenterMultiplayerAddAndStartVoiceChatChannel(string channelName);

	public static void addAndStartVoiceChatChannel(string channelName)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			_gameCenterMultiplayerAddAndStartVoiceChatChannel(channelName);
		}
	}

	[DllImport("__Internal")]
	private static extern void _gameCenterMultiplayerStartVoiceChat(string channelName, bool shouldEnable);

	public static void startVoiceChat(string channelName, bool shouldEnable)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			_gameCenterMultiplayerStartVoiceChat(channelName, shouldEnable);
		}
	}

	[DllImport("__Internal")]
	private static extern void _gameCenterMultiplayerEnableMicrophone(string channelName, bool shouldEnable);

	public static void enableMicrophone(string channelName, bool shouldEnable)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			_gameCenterMultiplayerEnableMicrophone(channelName, shouldEnable);
		}
	}

	[DllImport("__Internal")]
	private static extern void _gameCenterMultiplayerSetVolume(string channelName, float volume);

	public static void setVolume(string channelName, float volume)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			_gameCenterMultiplayerSetVolume(channelName, volume);
		}
	}

	[DllImport("__Internal")]
	private static extern void _gameCenterMultiplayerSetMute(string channelName, string playerId, bool shouldMute);

	public static void setMute(string channelName, string playerId, bool shouldMute)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			_gameCenterMultiplayerSetMute(channelName, playerId, shouldMute);
		}
	}

	[DllImport("__Internal")]
	private static extern void _gameCenterMultiplayerReceiveUpdates(string channelName, bool shouldReceiveUpdates);

	public static void receiveUpdates(string channelName, bool shouldReceiveUpdates)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			_gameCenterMultiplayerReceiveUpdates(channelName, shouldReceiveUpdates);
		}
	}

	[DllImport("__Internal")]
	private static extern void _gameCenterMultiplayerFindMatchProgrammaticallyWithMinMaxPlayers(int minPlayers, int maxPlayers);

	public static void findMatchProgrammaticallyWithMinMaxPlayers(int minPlayers, int maxPlayers)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			_gameCenterMultiplayerFindMatchProgrammaticallyWithMinMaxPlayers(minPlayers, maxPlayers);
		}
	}

	[DllImport("__Internal")]
	private static extern void _gameCenterMultiplayerFindMatchProgrammaticallyWithFilters(int minPlayers, int maxPlayers, uint playerGroup, uint playerAttributes);

	public static void findMatchProgrammaticallyWithFilters(int minPlayers, int maxPlayers, uint playerGroup, uint playerAttributes)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			_gameCenterMultiplayerFindMatchProgrammaticallyWithFilters(minPlayers, maxPlayers, playerGroup, playerAttributes);
		}
	}

	[DllImport("__Internal")]
	private static extern void _gameCenterMultiplayerCancelProgrammaticMatchRequest();

	public static void cancelProgrammaticMatchRequest()
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			_gameCenterMultiplayerCancelProgrammaticMatchRequest();
		}
	}

	[DllImport("__Internal")]
	private static extern void _gameCenterMultiplayerAddPlayersToCurrentMatch(string peerIds);

	public static void addPlayersToCurrentMatch(string[] playerIds)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			_gameCenterMultiplayerAddPlayersToCurrentMatch(string.Join(",", playerIds));
		}
	}

	[DllImport("__Internal")]
	private static extern void _gameCenterMultiplayerFindAllActivity();

	public static void findAllActivity()
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			_gameCenterMultiplayerFindAllActivity();
		}
	}

	[DllImport("__Internal")]
	private static extern void _gameCenterMultiplayerFindAllActivityForPlayerGroup(int playerGroup);

	public static void findAllActivityForPlayerGroup(int playerGroup)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			_gameCenterMultiplayerFindAllActivityForPlayerGroup(playerGroup);
		}
	}
}
#endif
