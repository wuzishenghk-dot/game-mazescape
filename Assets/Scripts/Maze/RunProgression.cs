using System.Collections.Generic;
using UnityEngine;
namespace MazeGame
{
    public partial class MazeGameManager
    {
        public int Wallet { get; private set; }
        public int MaxHealth => 3 + (_upgrades[3] ? 1 : 0);
        public int AttackDamage => _upgrades[0] ? 2 : 1;
        public float AttackReach => _upgrades[1] ? 1.7f : 1.35f;
        public float DashCooldown => _upgrades[2] ? 1.5f : 2.5f;
        public int PotionHealing => _upgrades[4] ? 2 : 1;
        public float FreezeDuration => _upgrades[5] ? 5f : 3f;
        private readonly bool[] _upgrades = new bool[6];
        private readonly int[] _choices = new int[3];
        private static readonly string[] UpgradeNames = { "锋利武器", "宽幅斩击", "轻盈步伐", "坚韧体魄", "药剂精通", "寒冰掌握" };
        private static readonly string[] UpgradeDescriptions = { "每次攻击伤害 1 → 2", "攻击距离 1.35 → 1.7", "冲刺冷却 2.5 → 1.5 秒", "生命上限 +1，恢复 1 点生命", "药水恢复 1 → 2 点生命", "冰冻时间 3 → 5 秒" };
        private int _checkpointLevel, _savedWallet, _savedHealth;
        private int[] _savedItems = new int[3];
        private bool[] _savedUpgrades = new bool[6];
        private string _shopMessage = "准备好后，可以直接前往下一关。";
        public void StartRun()
        {
            Wallet=0;Health=3;System.Array.Clear(_upgrades,0,6);System.Array.Clear(Inventory,0,3);
            _checkpointLevel=0;StartLevel(1);
        }
        private void EnterLevelProgress(int level)
        {
            if(_checkpointLevel==level)
            {
                Wallet=_savedWallet;Health=_savedHealth;System.Array.Copy(_savedItems,Inventory,3);System.Array.Copy(_savedUpgrades,_upgrades,6);
            }
            else
            {
                _checkpointLevel=level;_savedWallet=Wallet;_savedHealth=Mathf.Max(1,Health);
                _savedItems=(int[])Inventory.Clone();_savedUpgrades=(bool[])_upgrades.Clone();
            }
        }
        private void RollUpgrades()
        {
            var available=new List<int>();for(int i=0;i<6;i++)if(!_upgrades[i])available.Add(i);
            for(int i=0;i<3;i++){int n=Random.Range(0,available.Count);_choices[i]=available[n];available.RemoveAt(n);}
        }
        public void OpenUpgradeChoice()
        {
            if(CurrentState!=GameState.LevelCompleted||currentLevel>=5)return;
            CurrentState=GameState.UpgradeChoice;
        }
        public bool ChooseUpgrade(int slot)
        {
            if(CurrentState!=GameState.UpgradeChoice||slot<0||slot>2)return false;
            int kind=_choices[slot];if(_upgrades[kind])return false;
            _upgrades[kind]=true;if(kind==3)Health=Mathf.Min(MaxHealth,Health+1);
            _shopMessage="已获得："+UpgradeNames[kind];CurrentState=GameState.Shop;return true;
        }
        public bool BuySupply(int kind)
        {
            if(CurrentState!=GameState.Shop||kind<0||kind>3)return false;
            int cost=kind==3?3:kind==1?4:2;
            if(kind==3&&Health>=MaxHealth){_shopMessage="生命已满，无需休整。";return false;}
            if(Wallet<cost){_shopMessage="金币不足，可以直接继续冒险。";return false;}
            Wallet-=cost;if(kind==3)Health=MaxHealth;else Inventory[kind]++;
            _shopMessage=kind==3?"生命已恢复。":"已购买"+ItemArt.Names[kind];return true;
        }
        public void ContinueRun()
        {
            if(CurrentState!=GameState.Shop||currentLevel>=5)return;
            StartLevel(currentLevel+1);
        }
        public void ClaimChallengeReward()
        {
            if(CurrentState!=GameState.Playing)return;
            Wallet+=5;Inventory[Random.Range(0,3)]++;Notify("宝箱挑战完成！+5 金币，+1 随机道具");
        }
        private void DrawProgression(float width,float height)
        {
            GUI.color=new Color(0,0,0,.75f);GUI.DrawTexture(new Rect(0,0,width,height),Texture2D.whiteTexture);GUI.color=Color.white;
            GUILayout.BeginArea(new Rect((width-720)/2,(height-440)/2,720,440),_cardStyle);
            GUILayout.Space(12);GUILayout.Label(CurrentState==GameState.UpgradeChoice?"选择本次冒险的成长":"旅途补给站",_winTitleStyle);
            GUILayout.Space(16);GUILayout.Label($"钱包：{Wallet} 金币    生命：{Health}/{MaxHealth}",_labelStyle);
            if(CurrentState==GameState.UpgradeChoice)
            {
                GUILayout.Label("三选一 · 本次五关冒险内持续生效",_labelStyle);GUILayout.Space(16);
                for(int i=0;i<3;i++){int k=_choices[i];if(GUILayout.Button(UpgradeNames[k]+"  /  "+UpgradeDescriptions[k],_btnStyle,GUILayout.Height(65))){ChooseUpgrade(i);break;}}
            }
            else
            {
                GUILayout.Label(_shopMessage,_labelStyle);GUILayout.Space(14);
                for(int i=0;i<3;i++)if(GUILayout.Button(ItemArt.Names[i]+"  ×"+Inventory[i]+"    购买："+(i==1?4:2)+" 金币",_btnStyle,GUILayout.Height(42)))BuySupply(i);
                if(GUILayout.Button("营地休整 · 恢复全部生命 · 3 金币",_btnStyle,GUILayout.Height(42)))BuySupply(3);
                GUILayout.Space(16);if(GUILayout.Button("准备好了 · 前往下一关",_btnStyle,GUILayout.Height(48)))ContinueRun();
            }
            GUILayout.EndArea();
        }
    }
}
