using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SonyVitaCommonDialog : MonoBehaviour, IScreen
{
    MenuStack menuStack;
	float waitTime = 0;
	float progressDelay = 0;
	float progressTime = 0;
	string imeText = "こんにちは";

	MenuLayout menuMain;
	MenuLayout menuUserMessage;
	MenuLayout menuSystemMessage;
	MenuLayout menuErrorMessage;
	MenuLayout menuProgress;
	MenuLayout menuTRCMessage;

	void Start()
	{
		menuMain = new MenuLayout(this, 450, 34);
		menuUserMessage = new MenuLayout(this, 450, 34);
		menuSystemMessage = new MenuLayout(this, 450, 34);
		menuErrorMessage = new MenuLayout(this, 450, 34);
		menuProgress = new MenuLayout(this, 450, 34);
		menuTRCMessage = new MenuLayout(this, 600, 34);
		menuStack = new MenuStack();
		menuStack.SetMenu(menuMain);

		Sony.Vita.Dialog.Main.OnLog += OnLog;
		Sony.Vita.Dialog.Main.OnLogWarning += OnLogWarning;
		Sony.Vita.Dialog.Main.OnLogError += OnLogError;

		Sony.Vita.Dialog.Common.OnGotDialogResult += OnGotDialogResult;
		Sony.Vita.Dialog.Ime.OnGotIMEDialogResult += OnGotIMEDialogResult;

		Sony.Vita.Dialog.Main.Initialise();
	}

	public void OnEnter() {}
	public void OnExit() {}

	public void Process(MenuStack stack)
	{
		if(stack.GetMenu() == menuMain)
		{
			MenuMain();
		}
		else if (stack.GetMenu() == menuUserMessage)
		{
			MenuUserMessage();
		}
		else if (stack.GetMenu() == menuSystemMessage)
		{
			MenuSystemMessage();
		}
		else if (stack.GetMenu() == menuTRCMessage)
		{
			MenuTRCMessage();
		}
		else if (stack.GetMenu() == menuErrorMessage)
		{
			MenuErrorMessage();
		}
		else if (stack.GetMenu() == menuProgress)
		{
			MenuProgress();
		}
	}

	public void MenuMain()
	{
        menuMain.Update();

		if (menuMain.AddItem("IME Dialog"))
		{
			Sony.Vita.Dialog.Ime.ImeDialogParams info = new Sony.Vita.Dialog.Ime.ImeDialogParams();

			// Set supported languages, 'or' flags together or set to 0 to support all languages.
			info.supportedLanguages = Sony.Vita.Dialog.Ime.FlagsSupportedLanguages.LANGUAGE_JAPANESE |
										Sony.Vita.Dialog.Ime.FlagsSupportedLanguages.LANGUAGE_ENGLISH_GB |
										Sony.Vita.Dialog.Ime.FlagsSupportedLanguages.LANGUAGE_DANISH;
			info.languagesForced = true;

			info.type = Sony.Vita.Dialog.Ime.EnumImeDialogType.TYPE_DEFAULT;
			info.option = 0;
			info.canCancel = true;
			info.textBoxMode = Sony.Vita.Dialog.Ime.FlagsTextBoxMode.TEXTBOX_MODE_WITH_CLEAR;
			info.enterLabel = Sony.Vita.Dialog.Ime.EnumImeDialogEnterLabel.ENTER_LABEL_DEFAULT;
			info.maxTextLength = 128;
			info.title = "日本語";
			info.initialText = imeText;
			Sony.Vita.Dialog.Ime.Open(info);
		} 

        if (menuMain.AddItem("User"))
		{
			menuStack.PushMenu(menuUserMessage);
		}

        if (menuMain.AddItem("System"))
		{
			menuStack.PushMenu(menuSystemMessage);
		}

		if (menuMain.AddItem("TRC Messages"))
		{
			menuStack.PushMenu(menuTRCMessage);
		}

		if (menuMain.AddItem("Progress"))
		{
			menuStack.PushMenu(menuProgress);
		}

        if (menuMain.AddItem("Error"))
		{
			menuStack.PushMenu(menuErrorMessage);
		}
	}

	void MenuUserMessage()
	{
        menuUserMessage.Update();

        if (menuUserMessage.AddItem("Yes No"))
		{
			Sony.Vita.Dialog.Common.ShowUserMessage(Sony.Vita.Dialog.Common.EnumUserMessageType.MSG_DIALOG_BUTTON_TYPE_YESNO, true, "Do Something ?");
		}

        if (menuUserMessage.AddItem("Ok"))
		{
			Sony.Vita.Dialog.Common.ShowUserMessage(Sony.Vita.Dialog.Common.EnumUserMessageType.MSG_DIALOG_BUTTON_TYPE_OK, true, "Do Something ?");
		}

        if (menuUserMessage.AddItem("Ok Cancel"))
		{
			Sony.Vita.Dialog.Common.ShowUserMessage(Sony.Vita.Dialog.Common.EnumUserMessageType.MSG_DIALOG_BUTTON_TYPE_OK_CANCEL, true, "Do Something ?");
		}

        if (menuUserMessage.AddItem("Cancel"))
		{
			Sony.Vita.Dialog.Common.ShowUserMessage(Sony.Vita.Dialog.Common.EnumUserMessageType.MSG_DIALOG_BUTTON_TYPE_CANCEL, true, "Do Something ?");
		}

        if (menuUserMessage.AddItem("No Button"))
		{
			Sony.Vita.Dialog.Common.ShowUserMessage(Sony.Vita.Dialog.Common.EnumUserMessageType.MSG_DIALOG_BUTTON_TYPE_NONE, true, "Do Something ?");
			waitTime = 5;
		}

        if (menuUserMessage.AddItem("3 Buttons"))
		{
			Sony.Vita.Dialog.Common.ShowUserMessage3Button(true, "Pick a button, any button.", "Button 1", "Button 2", "Button 3");
		}

        if (menuUserMessage.AddItem("Back"))
		{
			menuStack.PopMenu();
		}
	}

	void MenuSystemMessage()
	{
        menuSystemMessage.Update();

        if (menuSystemMessage.AddItem("No Space"))
		{
			Sony.Vita.Dialog.Common.ShowSystemMessage(Sony.Vita.Dialog.Common.EnumSystemMessageType.MSG_DIALOG_SYSMSG_TYPE_NOSPACE, true, 100);
		}

        if (menuSystemMessage.AddItem("No Space Continue"))
		{
			Sony.Vita.Dialog.Common.ShowSystemMessage(Sony.Vita.Dialog.Common.EnumSystemMessageType.MSG_DIALOG_SYSMSG_TYPE_NOSPACE_CONTINUABLE, true, 100);
		}

        if (menuSystemMessage.AddItem("Compass Calibrate"))
		{
			Sony.Vita.Dialog.Common.ShowSystemMessage(Sony.Vita.Dialog.Common.EnumSystemMessageType.MSG_DIALOG_SYSMSG_TYPE_MAGNETIC_CALIBRATION, true, 0);
		}

        if (menuSystemMessage.AddItem("Wait"))
		{
			Sony.Vita.Dialog.Common.ShowSystemMessage(Sony.Vita.Dialog.Common.EnumSystemMessageType.MSG_DIALOG_SYSMSG_TYPE_WAIT, true, 0);
			waitTime = 5;
		}

        if (menuSystemMessage.AddItem("Wait Small"))
		{
			Sony.Vita.Dialog.Common.ShowSystemMessage(Sony.Vita.Dialog.Common.EnumSystemMessageType.MSG_DIALOG_SYSMSG_TYPE_WAIT_SMALL, true, 0);
			waitTime = 5;
		}

        if (menuSystemMessage.AddItem("Wait Cancel"))
		{
			Sony.Vita.Dialog.Common.ShowSystemMessage(Sony.Vita.Dialog.Common.EnumSystemMessageType.MSG_DIALOG_SYSMSG_TYPE_WAIT_CANCEL, true, 0);
			waitTime = 5;
		}

        if (menuSystemMessage.AddItem("Patch Found"))
        {
            Sony.Vita.Dialog.Common.ShowSystemMessage(Sony.Vita.Dialog.Common.EnumSystemMessageType.MSG_DIALOG_SYSMSG_TYPE_PATCH_FOUND, true, 0);
        }

        if (menuSystemMessage.AddItem("Back"))
		{
			menuStack.PopMenu();
		}
	}

	void MenuTRCMessage()
	{
		menuTRCMessage.Update();

		if (menuTRCMessage.AddItem("TRC Empty Store"))
		{
			Sony.Vita.Dialog.Common.ShowSystemMessage(Sony.Vita.Dialog.Common.EnumSystemMessageType.MSG_DIALOG_SYSMSG_TYPE_TRC_EMPTY_STORE, true, 0);
		}

		if (menuTRCMessage.AddItem("TRC PSN Age Restricted"))
		{
			Sony.Vita.Dialog.Common.ShowSystemMessage(Sony.Vita.Dialog.Common.EnumSystemMessageType.MSG_DIALOG_SYSMSG_TYPE_TRC_PSN_AGE_RESTRICTION, true, 0);
			waitTime = 5;
		}

		if (menuTRCMessage.AddItem("TRC PSN Chat Restricted"))
		{
			Sony.Vita.Dialog.Common.ShowSystemMessage(Sony.Vita.Dialog.Common.EnumSystemMessageType.MSG_DIALOG_SYSMSG_TYPE_TRC_PSN_CHAT_RESTRICTION, true, 0);
			waitTime = 5;
		}

		if (menuTRCMessage.AddItem("TRC Mic Disabled"))
		{
			Sony.Vita.Dialog.Common.ShowSystemMessage(Sony.Vita.Dialog.Common.EnumSystemMessageType.MSG_DIALOG_SYSMSG_TYPE_TRC_MIC_DISABLED, true, 0);
			waitTime = 5;
		}

		if (menuTRCMessage.AddItem("TRC Mic Disabled Cont"))
		{
			Sony.Vita.Dialog.Common.ShowSystemMessage(Sony.Vita.Dialog.Common.EnumSystemMessageType.MSG_DIALOG_SYSMSG_TYPE_TRC_MIC_DISABLED_CONTINUABLE, true, 0);
			waitTime = 5;
		}

		if (menuTRCMessage.AddItem("Back"))
		{
			menuStack.PopMenu();
		}
	}
	
	void MenuErrorMessage()
	{
        menuErrorMessage.Update();

        if (menuErrorMessage.AddItem("Error Message"))
		{
			Sony.Vita.Dialog.Common.ShowErrorMessage(0x8001000C);
		}

        if (menuErrorMessage.AddItem("Back"))
		{
			menuStack.PopMenu();
		}
	}


	void MenuProgress()
	{
        menuProgress.Update();

        if (menuProgress.AddItem("Progress Bar"))
		{
			Sony.Vita.Dialog.Common.ShowProgressBar("Working");
			progressDelay = 3;
			progressTime = 5;
		}

        if (menuProgress.AddItem("Back"))
		{
			menuStack.PopMenu();
		}
	}

	void OnGUI()
	{
		MenuLayout activeMenu = menuStack.GetMenu();
		activeMenu.GetOwner().Process(menuStack);
	}

	void OnLog(Sony.Vita.Dialog.Messages.PluginMessage msg)
	{
		OnScreenLog.Add(msg.Text);
	}

    void OnLogWarning(Sony.Vita.Dialog.Messages.PluginMessage msg)
	{
		OnScreenLog.Add("WARNING: " + msg.Text);
	}

    void OnLogError(Sony.Vita.Dialog.Messages.PluginMessage msg)
	{
		OnScreenLog.Add("ERROR: " + msg.Text);
	}

    void OnGotDialogResult(Sony.Vita.Dialog.Messages.PluginMessage msg)
    {
        Sony.Vita.Dialog.Common.EnumCommonDialogResult result = Sony.Vita.Dialog.Common.GetResult();

        OnScreenLog.Add("Dialog result: " + result);
    }
    
    void OnGotIMEDialogResult(Sony.Vita.Dialog.Messages.PluginMessage msg)
    {
		Sony.Vita.Dialog.Ime.ImeDialogResult result = Sony.Vita.Dialog.Ime.GetResult();

        OnScreenLog.Add("IME result: " + result.result);
        OnScreenLog.Add("IME button: " + result.button);
        OnScreenLog.Add("IME text: " + result.text);
		if (result.result == Sony.Vita.Dialog.Ime.EnumImeDialogResult.RESULT_OK)
		{
			imeText = result.text;
		}
    }
	
	void Update ()
    {
        Sony.Vita.Dialog.Main.Update();

		// Update system wait dialog.
		if(waitTime > 0)
		{
			waitTime -= Time.deltaTime;
			if (waitTime <= 0)
			{
				waitTime = 0;
				Sony.Vita.Dialog.Common.Close();
			}
		}

		// Update progress dialog.
		if(progressDelay > 0)
		{
			progressDelay -= Time.deltaTime;
			if (progressDelay <= 0)
			{
				progressDelay = 0;
			}
		}
		else if (progressTime > 0)
		{
			progressTime -= Time.deltaTime;
			if (progressTime <= 0)
			{
				progressTime = 0;
				Sony.Vita.Dialog.Common.Close();
			}

			float percent = (5 - progressTime) / 5;
			int intPercent = (int)(percent * 100);
			Sony.Vita.Dialog.Common.SetProgressBarPercent(intPercent);
			Sony.Vita.Dialog.Common.SetProgressBarMessage("Coming Soon - " + intPercent);
		}
	}

}
