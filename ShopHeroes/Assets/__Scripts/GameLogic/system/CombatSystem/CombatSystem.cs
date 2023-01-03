using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class CombatSystem : BaseSystem
{
    private CombatReport combatReport; //��ǰս������
    public int currComBatSceneId = 1; //��ǰ����id
    public int combatType = 1; //ս������ 1 - ���� 2 - ����
    public string curCombatSceneName = "";
    private List<FighterClr> allFighterList = new List<FighterClr>();

    private float playSpeed = 1.0f;     //�����ٶ�
    private CombatMapInfo combatMapInfo;
    private CombatView combatView;
    private CombatActionCrl combatActionCrl;

    private bool isSkip = false;
    protected override void AddListeners()
    {
        EventController.inst.AddListener(GameEventType.CombatEvent.COMBAT_PLAY_REPORT, StartComBat);
        EventController.inst.AddListener(GameEventType.CombatEvent.COMBAT_INITSCENE, InitCombatScene);
        EventController.inst.AddListener(GameEventType.CombatEvent.COMBAT_EXIT, EndComBat);
        EventController.inst.AddListener<int>(GameEventType.CombatEvent.COMBAT_VIEW_HPUPDATE, updateUICard);
        EventController.inst.AddListener<int, int, string, CombatReport>(GameEventType.CombatEvent.COMBAT_SETANDINTOCOMBAT, SetAndPlayCombatReport);
        Helper.AddNetworkRespListener(MsgType.Response_Gm_Command_Cmd, CombatInfoUpdate);
        EventController.inst.AddListener<float>(GameEventType.CombatEvent.COMBAT_PLAYSPEED_CHANGE, setPlaySpeed);
        EventController.inst.AddListener<int>(GameEventType.CombatEvent.COMBAT_USE_ANGER, UseAngerSkill);
        EventController.inst.AddListener(GameEventType.CombatEvent.COMBAT_USE_SKIP, SkipCombat);
        //VFXTest
#if UNITY_EDITOR
        EventController.inst.AddListener<int, int>(GameEventType.CombatEvent.COMBAT_ADDBUFF_TEST, AddBuff);
        EventController.inst.AddListener<int, int>(GameEventType.CombatEvent.COMBAT_USESKILL_TEST, playSkill);
#endif
    }
    protected override void RemoveListeners()
    {
        EventController.inst.RemoveListener(GameEventType.CombatEvent.COMBAT_PLAY_REPORT, StartComBat);
        EventController.inst.RemoveListener(GameEventType.CombatEvent.COMBAT_INITSCENE, InitCombatScene);
        EventController.inst.RemoveListener(GameEventType.CombatEvent.COMBAT_EXIT, EndComBat);
        EventController.inst.RemoveListener<int>(GameEventType.CombatEvent.COMBAT_VIEW_HPUPDATE, updateUICard);
        EventController.inst.RemoveListener<int, int, string, CombatReport>(GameEventType.CombatEvent.COMBAT_SETANDINTOCOMBAT, SetAndPlayCombatReport);
        EventController.inst.RemoveListener<float>(GameEventType.CombatEvent.COMBAT_PLAYSPEED_CHANGE, setPlaySpeed);
        EventController.inst.RemoveListener<int>(GameEventType.CombatEvent.COMBAT_USE_ANGER, UseAngerSkill);
        EventController.inst.RemoveListener(GameEventType.CombatEvent.COMBAT_USE_SKIP, SkipCombat);
#if UNITY_EDITOR
        EventController.inst.RemoveListener<int, int>(GameEventType.CombatEvent.COMBAT_ADDBUFF_TEST, AddBuff);
        EventController.inst.RemoveListener<int, int>(GameEventType.CombatEvent.COMBAT_USESKILL_TEST, playSkill);
#endif
    }
    ////////////////////////////////////���Դ���///////////////////////////////////////////////////////////////////////////////
#if UNITY_EDITOR
    private void AddBuff(int key, int buffid)
    {
        FighterClr targetFighter = allFighterList.Find(item => item.key == key);
        //if (!targetFighter.HaveBuff(buffid))
        //{
        targetFighter.AddBuffer(buffid);
        //}
    }

    private void removeBuff(int key, int buffid)
    {
        FighterClr targetFighter = allFighterList.Find(item => item.key == key);
        if (!targetFighter.HaveBuff(buffid))
        {
            targetFighter.RemoveBuffer(buffid);
        }
    }
    int skilltesttimer = 0;
    private void playSkill(int skillid, int weapon)
    {
        GameTimer.inst.RemoveTimer(skilltesttimer);
        CombatAction action = new CombatAction();
        action.actionId = 4;
        action.actionType = (int)BattleActionType.skill;
        action.skillId = skillid;
        action.actionFrom.key = 4;
        action.actionFrom.weapon = weapon;
        CombatTarget ct = new CombatTarget();
        ct.key = 7;
        CombatTargetEffect cte = new CombatTargetEffect();
        cte.hited = 1;
        cte.hp = -1;
        ct.effectList.Add(cte);
        action.actionTarget.Add(ct);
        combatActionCrl.PlayAction(action);

        skilltesttimer = GameTimer.inst.AddTimer(5f, () =>
        {
            combatActionCrl.PlayAction(action);
        });
    }
#endif
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //���ų�����Ч
    private void CreateSceneVfx(int vfxid, Vector3 pos)
    {
        VFXConfig vfxCfg = VFXConfigManager.inst.GetConfig(vfxid);
    }
    //�յ�ս��
    private void CombatInfoUpdate(HttpMsgRspdBase msg)
    {
        Response_Gm_Command data = (Response_Gm_Command)msg;
        if (data.errorCode == (int)EErrorCode.EEC_Success)
        {
            if (data.testReport == null) return;
            if (data.testReport.waves.Count == 0) return;
            combatReport = data.testReport;
#if UNITY_EDITOR
            var path = Application.dataPath.Replace("/Assets", "") + "/combatReport.txt";
            //SaveManager.Save<string>(msg.GetJsonParams(), path);//msg.GetJsonParams()
            Logger.log("combatReport" + combatReport.GetJsonData().ToString());
            // if (!File.Exists(path))
            {
                File.WriteAllText(path, msg.GetJsonParams(), Encoding.UTF8);
            }
#endif
            //����ط�ս��״̬
            //GameStateEvent.inst.changeState(new StateTransition(kGameState.Battle, false));
            NetworkManager.inst.PauseKeepAlive(true); //ս�����̲���Ҫ����
            HotfixBridge.inst.ChangeState(new StateTransition(kGameState.Battle, true));
        }
    }

    // combatType == 1 ���� combatType == 2 ����
    private void SetAndPlayCombatReport(int levelid, int combatType, string sceneName, CombatReport report)
    {
        currComBatSceneId = levelid;
        this.combatType = combatType;
        combatReport = report;
        curCombatSceneName = sceneName;

        //����ط�ս��״̬
        //GameStateEvent.inst.changeState(new StateTransition(kGameState.Battle, false));
        NetworkManager.inst.PauseKeepAlive(true); //ս�����̲���Ҫ����
        HotfixBridge.inst.ChangeState(new StateTransition(kGameState.Battle, true));
    }
    //��ʼս��
    private void StartComBat()
    {
        if (combatReport != null)
        {
            //����ս��ģʽ
            if (ManagerBinder.inst.mGameState != kGameState.Town)
            {
                //ֻ���ڳ��Ј����ſ����M��ս��
                return;
            }
            NetworkManager.inst.PauseKeepAlive(true); //ս�����̲���Ҫ����
            HotfixBridge.inst.ChangeState(new StateTransition(kGameState.Battle, true));
        }
    }
    //��ʼ��ս������
    private void InitCombatScene()
    {
        LoadScene();
    }
    private void LoadScene()
    {
        if (combatType == 1)
        {
            ExploreInstanceConfigData instanceCfg = ExploreInstanceConfigManager.inst.GetConfig(currComBatSceneId);
            if (instanceCfg != null)
            {
                string scenename = instanceCfg.scenes;

                ManagerBinder.inst.mSceneMgr.loadSceneAsync(scenename, UnityEngine.SceneManagement.LoadSceneMode.Additive, () =>
                {
                    if (combatMapInfo == null) combatMapInfo = new CombatMapInfo();
                    combatMapInfo.initMapInfo();
                    //�����������
                    InitFighter();
                    EventController.inst.TriggerEvent(GameEventType.CombatEvent.COMBAT_SCENE_LOADED);
                });
            }
            else
            {
                Debug.LogError("û�ж�Ӧ�������� instanceCfg==" + instanceCfg);
            }
        }
        else if (combatType == 2)
        {
            if (curCombatSceneName != null && curCombatSceneName.Length > 0)
            {
                string scenename = curCombatSceneName;

                ManagerBinder.inst.mSceneMgr.loadSceneAsync(scenename, UnityEngine.SceneManagement.LoadSceneMode.Additive, () =>
                {
                    if (combatMapInfo == null) combatMapInfo = new CombatMapInfo();
                    combatMapInfo.initMapInfo();
                    //�����������
                    InitFighter();
                    EventController.inst.TriggerEvent(GameEventType.CombatEvent.COMBAT_SCENE_LOADED);
                });
            }

        }

    }
    void updateUICard(int key)
    {
        var fighter = allFighterList.Find(f => f.key == key);
        if (combatView != null)
        {
            combatView.updateAnger(key, fighter.currAnger);
            combatView.updateHp(key, fighter.currHp);
        }
    }

    void UseAngerSkill(int key)
    {
        combatView.UseAngerSkill(key);
        combatMapInfo.skillBlackPlane.gameObject.SetActive(true);
    }
    private void InitFighter()
    {
        // GUIManager.HideView<TopPlayerInfoView>();
        combatView = GUIManager.OpenView<CombatView>((view) =>
        {
            if (view != null)
            {
                view.setState(combatType);
                view.setRound(1);
                combatView = view;
                GameTimer.inst.AddTimerFrame(10, 1, ToBattle);
                //ToBattle();
            }
        });
    }

    //��ʼս��
    public void ToBattle()
    {
        AudioManager.inst.PlayMusic(66);
        D2DragCamera.inst.SetVolumeBloomActive(true, true);

        combatActionCrl = new CombatActionCrl();
        allFighterList.Clear();
        FGUI.inst.showGlobalMask(3f);
        if (combatReport != null && combatReport.waves.Count > 0)
        {
            playSpeed = GameSettingManager.combatPlaySpeed;
            isSkip = false;
            GameTimer.inst.StartCoroutine(BattleUpdate());
        }
    }

    public void SkipCombat()
    {
        if (isSkip) return;
        isSkip = true;
    }
    IEnumerator BattleUpdate()
    {
        yield return new WaitForSeconds(0.5f);
        while (!combatView.isShowing)
        {
            yield return null;
        }
        if (combatView != null)
        {
            combatView.InitFighter(combatReport.info.attackers);
        }
        //yield return new WaitForSeconds(0.5f);
        // yield break;
        for (int wave = 0; wave < combatReport.waves.Count; wave++)
        {
            CombatWave combatwave = combatReport.waves[wave];
            Logger.log($"��ǰ��{combatwave.waveId}��ս��");
            if (combatView != null)
            {
                combatView.refreshState(2, combatwave.waveId);
            }
            allFighterList.ForEach((f) =>
            {
                if (f.isDeath && !f.isLeft)
                {
                    f.DeletSelf();
                }
            });
            if (wave == 0)
            {
                //�ҷ�
                foreach (CombatFighter fighter in combatReport.info.attackers)
                {
                    if (allFighterList.Find(f => f.key == fighter.key) != null)
                    {
                        continue;
                    }
                    FighterClr fc = FighterClr.CreatFighter(fighter, combatMapInfo.GetSiteTF(fighter.site, false), true);
                    fc.speedRatio = playSpeed;
                    allFighterList.Add(fc);
                }
            }
            //�з�
            foreach (int key in combatwave.adversaryKeys)
            {
                CombatFighter fighter = combatReport.info.adversarys.Find(f => f.key == key);
                if (fighter != null)
                {
                    FighterClr fc = FighterClr.CreatFighter(fighter, combatMapInfo.GetSiteTF(fighter.site, true), false);
                    fc.speedRatio = playSpeed;
                    allFighterList.Add(fc);
                }
            }
            combatActionCrl.FighterList = allFighterList;
            yield return new WaitForSeconds(0.2f);

            //��ʱ���Ե������
            combatView.showBtn();
            FGUI.inst.showGlobalMask(0.2f);
            //ս������
            // yield return new WaitForSeconds(0.5f);
            // if (combatView != null)
            // {
            //     combatView.BattleStart();
            // }
            if (isSkip)
            {
                break;
            }
            // yield return new WaitForSeconds(1f / playSpeed);
            List<CombatRound> rounds = combatwave.rounds;
            for (int i = 0; i < rounds.Count; i++)
            {
                Logger.log($"��{rounds[i].roundId}�غ�");
                if (combatView != null)
                    combatView.setRound(rounds[i].roundId);
                yield return new WaitForSeconds(0.5f / playSpeed);
                yield return BattleAction(rounds[i]);
                if (isSkip)
                {
                    break;
                }
            }
            //��һ��
            if (isSkip)
            {
                break;
            }
        }
        yield return new WaitForSeconds(0.5f / playSpeed);
        CombatResult result = combatReport.result;

        bool isFinish = true;
        //ʤ��
        result.attacker.ForEach((fighter) =>
        {
            FighterClr _fighter = allFighterList.Find(item => item.key == fighter.key);
            if (_fighter != null)
            {
                if (fighter.dead != 0)
                {
                    _fighter.Death();
                }
                else
                {
                    if (result.winSide == 1)
                    {
                        _fighter.Cheer();
                    }
                }
            }
        });
        //ʧ��
        result.adversary.ForEach((fighter) =>
        {
            if (fighter != null)
            {
                FighterClr _fighter = allFighterList.Find(item => item.key == fighter.key);
                if (_fighter != null)
                {
                    if (fighter.dead != 0)
                    {
                        _fighter.Death();
                    }
                    else
                    {
                        isFinish = false;
                    }
                }
            }
        });

        if (combatView != null)
        {
            combatView.endState(isFinish);
        }

        yield return new WaitForSeconds(1f / playSpeed);
        // AudioManager.inst.PausedMusic();
        AudioManager.inst.stopAll();
        if (combatView != null)
        {
            combatView = null;
            GUIManager.HideView<CombatView>();
            //combatView.hide(); //�ر�ս������
        }
        AudioManager.inst.PlaySound(result.winSide == 1 ? 60 : 61);
        D2DragCamera.inst.SetVolumeBloomActive(false, true);
        if (combatType == 1)
        {
            if (ExploreDataProxy.inst.HasDamagedEquip)
                EventController.inst.TriggerEvent(GameEventType.ReceiveEvent.NEWITEM_MSG, new PopUIInfoBase { type = ReceiveInfoUIType.ExploreEquipDamaged });
            EventController.inst.TriggerEvent(GameEventType.ReceiveEvent.NEWITEM_MSG, new PopUIInfoBase { type = ReceiveInfoUIType.ExploreAward });
        }
        else if (combatType == 2)
        {
            HotfixBridge.inst.TriggerLuaEvent("OpenUI_RuinsFinishUI");
        }
    }

    IEnumerator PlayAction(CombatAction action)
    {
        combatActionCrl.PlayAction(action);
        while (!combatActionCrl.ActionEnd(action.actionId))
        {
            yield return null;
        }
        combatMapInfo.skillBlackPlane.gameObject.SetActive(false);
    }
    IEnumerator BattleAction(CombatRound rounds)
    {
        //ˢ�»غ���
        yield return new WaitForSeconds(0.5f / playSpeed);
        //�غ�ǰBUFF����
        foreach (CombatAction action in rounds.beforeActions)
        {
            if (isSkip)
            {
                yield break;
            }
            yield return PlayAction(action);
        }
        yield return new WaitForSeconds(0.5f / playSpeed);
        //ս������
        foreach (CombatAction action in rounds.actions)
        {
            if (isSkip)
            {
                yield break;
            }
            yield return PlayAction(action);
        }
        //�غϺ�buff����
        foreach (CombatAction action in rounds.endActions)
        {
            if (isSkip)
            {
                yield break;
            }
            yield return PlayAction(action);
        }
    }


    //����ս��
    private void EndComBat()
    {

        if (combatReport != null)
        {
            combatReport = null;
        }
        allFighterList.ForEach((f) =>
        {
            f.DeletSelf();
        });
        var topview = GUIManager.GetWindow<TopPlayerInfoView>();
        if (topview != null)
        {
            topview.UpdateShow();
        }
        if (combatType == 1)
        {
            ExploreInstanceConfigData instanceCfg = ExploreInstanceConfigManager.inst.GetConfig(currComBatSceneId);
            if (instanceCfg != null)
            {
                string scenename = instanceCfg.scenes;
                ManagerBinder.inst.mSceneMgr.UnLoadScene(scenename);
            }
        }
        else if (combatType == 2)
        {
            if (curCombatSceneName != null && curCombatSceneName.Length > 0)
            {
                string scenename = curCombatSceneName;
                ManagerBinder.inst.mSceneMgr.UnLoadScene(scenename);
            }
        }

        NetworkManager.inst.PauseKeepAlive(false); //������
    }

    //�����ٶ�
    public void setPlaySpeed(float speed)
    {
        if (playSpeed != speed)
        {
            playSpeed = speed;
            GameSettingManager.combatPlaySpeed = playSpeed;
            PlayerPrefs.SetFloat("combatPlaySpeed", playSpeed);
            combatActionCrl.setSpeedRatio(playSpeed);
            foreach (var fighter in allFighterList)
            {
                fighter.speedRatio = playSpeed;
            }
        }
    }
}
