using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using MirCommon;
using MirCommon.Network;
using MirCommon.Utils;

namespace GameServer
{
    
    
    
    public class PetSystem
    {
        private readonly HumanPlayer _owner;
        private readonly List<Monster> _pets = new();
        private Monster? _mainPet;
        private readonly object _petLock = new();
        
        
        private readonly Inventory _petBag = new() { MaxSlots = 10 };
        
        public PetSystem(HumanPlayer owner)
        {
            _owner = owner;
        }
        
        
        
        
        public int GetPetCount()
        {
            lock (_petLock)
            {
                return _pets.Count;
            }
        }
        
        
        
        
        public int MaxPets => 5; 
        
        
        
        
        public bool SummonPet(string petName, bool setOwner = true, int x = -1, int y = -1)
        {
            if (_pets.Count >= 5) 
            {
                _owner.Say("宠物数量已达上限");
                return false;
            }
            
            
            var pet = new Monster(0, petName) 
            {
                OwnerPlayerId = setOwner ? _owner.ObjectId : 0,
                IsPet = true
            };
            
            
            if (x == -1 || y == -1)
            {
                x = _owner.X;
                y = _owner.Y;
            }
            
            
            if (_owner.CurrentMap != null)
            {
                _owner.CurrentMap.AddObject(pet, (ushort)x, (ushort)y);
            }
            
            lock (_petLock)
            {
                _pets.Add(pet);
                if (_mainPet == null)
                {
                    _mainPet = pet;
                }
            }
            
            _owner.Say($"召唤了 {petName}");
            return true;
        }
        
        
        
        
        public bool ReleasePet(string petName)
        {
            lock (_petLock)
            {
                var pet = _pets.FirstOrDefault(p => p.Name == petName);
                if (pet == null)
                {
                    _owner.Say($"没有找到宠物 {petName}");
                    return false;
                }
                
                
                pet.CurrentMap?.RemoveObject(pet);
                _pets.Remove(pet);
                
                if (_mainPet == pet)
                {
                    _mainPet = _pets.FirstOrDefault();
                }
                
                _owner.Say($"释放了 {petName}");
                return true;
            }
        }
        
        
        
        
        public void SetPetTarget(AliveObject target)
        {
            lock (_petLock)
            {
                foreach (var pet in _pets)
                {
                    pet.SetTarget(target);
                }
            }
        }
        
        
        
        
        public void CleanPets()
        {
            lock (_petLock)
            {
                foreach (var pet in _pets)
                {
                    pet.CurrentMap?.RemoveObject(pet);
                }
                _pets.Clear();
                _mainPet = null;
            }
        }
        
        
        
        
        public Inventory GetPetBag() => _petBag;
        
        
        
        
        public bool SetPetBagSize(int size)
        {
            if (size != 5 && size != 10 && size != 0)
                return false;
                
            _petBag.MaxSlots = size;
            SendPetBagInfo();
            return true;
        }
        
        
        
        
        public bool GetItemFromPetBag(ulong makeIndex)
        {
            var item = _petBag.FindItem(makeIndex);
            if (item == null)
                return false;
                
            if (!_owner.Inventory.AddItem(item))
            {
                _owner.Say("背包已满");
                return false;
            }
            
            _petBag.RemoveItem(makeIndex, 1);
            SendPetBagInfo();
            return true;
        }
        
        
        
        
        public bool PutItemToPetBag(ulong makeIndex)
        {
            var item = _owner.Inventory.FindItem(makeIndex);
            if (item == null)
                return false;
                
            if (!_petBag.AddItem(item))
            {
                _owner.Say("宠物背包已满");
                return false;
            }
            
            _owner.Inventory.RemoveItem(makeIndex, 1);
            SendPetBagInfo();
            return true;
        }
        
        
        
        
        private void SendPetBagInfo()
        {
            
            
            
            
            
            SendSetPetBag((ushort)_petBag.MaxSlots);
            
            
            SendPetBag();
        }
        
        
        
        
        private void SendSetPetBag(ushort size)
        {
            var builder = new PacketBuilder();
            builder.WriteUInt32(_owner.ObjectId);
            builder.WriteUInt16(0x9602); 
            builder.WriteUInt16(0);
            builder.WriteUInt16(0);
            builder.WriteUInt16(0);
            builder.WriteUInt16(size);
            
            _owner.SendMessage(builder.Build());
        }
        
        
        
        
        private void SendPetBag()
        {
            var builder = new PacketBuilder();
            builder.WriteUInt32(_owner.ObjectId);
            builder.WriteUInt16(0x9603); 
            builder.WriteUInt16(0);
            builder.WriteUInt16(0);
            builder.WriteUInt16(0);
            
            
            var items = _petBag.GetAllItems();
            builder.WriteUInt16((ushort)_petBag.MaxSlots);
            builder.WriteUInt16((ushort)items.Count);
            
            
            foreach (var item in items.Values)
            {
                builder.WriteUInt64((ulong)item.InstanceId);
                builder.WriteUInt16((ushort)item.Definition.ItemId);
                builder.WriteUInt16((ushort)item.Durability);
                builder.WriteUInt16((ushort)item.MaxDurability);
                builder.WriteUInt32(item.Definition.SellPrice);
                builder.WriteByte(0); 
                builder.WriteByte(0); 
                builder.WriteByte(0); 
                builder.WriteByte(0); 
            }
            
            _owner.SendMessage(builder.Build());
        }
        
        
        
        
        public void ShowPetInfo()
        {
            lock (_petLock)
            {
                _owner.Say($"宠物数量: {_pets.Count}");
                foreach (var pet in _pets)
                {
                    _owner.Say($"{pet.Name} - 等级: {pet.Level} HP: {pet.CurrentHP}/{pet.MaxHP}");
                }
            }
        }

        
        
        
        public object GetPetInfo()
        {
            lock (_petLock)
            {
                
                var petInfo = new
                {
                    PetCount = _pets.Count,
                    MainPet = _mainPet?.Name ?? "无",
                    Pets = _pets.Select(p => new
                    {
                        Name = p.Name,
                        Level = p.Level,
                        HP = $"{p.CurrentHP}/{p.MaxHP}",
                        IsMain = p == _mainPet
                    }).ToList(),
                    PetBagSize = _petBag.MaxSlots,
                    PetBagUsed = _petBag.GetUsedSlots()
                };
                
                return petInfo;
            }
        }
        
        
        
        
        public void DistributePetExp(uint exp)
        {
            lock (_petLock)
            {
                if (_pets.Count == 0)
                    return;
                    
                uint expPerPet = exp / (uint)_pets.Count;
                foreach (var pet in _pets)
                {
                    
                    
                    _owner.Say($"{pet.Name} 获得 {expPerPet} 经验");
                }
            }
        }
    }
    
    
    
    
    public class MountSystem
    {
        private readonly HumanPlayer _owner;
        private MonsterEx? _horse;
        private bool _isRiding;
        private bool _horseRest;
        
        public MountSystem(HumanPlayer owner)
        {
            _owner = owner;
        }
        
        
        
        
        public MonsterEx? GetHorse() => _horse;
        
        
        
        
        public void SetHorse(MonsterEx? horse)
        {
            _horse = horse;
            if (_horse == null)
            {
                _isRiding = false;
            }
        }
        
        
        
        
        public bool RideHorse()
        {
            if (_horse == null)
            {
                _owner.Say("你没有坐骑");
                return false;
            }
            
            if (_horse.CurrentHP <= 0)
            {
                _owner.Say("坐骑已死亡");
                return false;
            }
            
            _isRiding = true;
            _owner.Say("骑乘坐骑");
            _owner.NotifyAppearanceChanged();
            return true;
        }
        
        
        
        
        public void Dismount()
        {
            _isRiding = false;
            _owner.Say("下马");
            _owner.NotifyAppearanceChanged();
        }
        
        
        
        
        public bool IsRiding() => _isRiding;
        
        
        
        
        public byte GetRunSpeed()
        {
            if (_isRiding) return 3; 
            return 2; 
        }
        
        
        
        
        public bool IsEquipedHorse()
        {
            
            var horseItem = _owner.Equipment.GetItem(EquipSlot.Mount);
            return horseItem != null;
        }
        
        
        
        
        public ItemInstance? GetEquipedHorseItem()
        {
            return _owner.Equipment.GetItem(EquipSlot.Mount);
        }
        
        
        
        
        public bool TrainHorse(int dir)
        {
            if (_horse == null)
            {
                _owner.Say("你没有坐骑");
                return false;
            }
            
            
            if (!_owner.CanDoAction(ActionType.Attack))
            {
                _owner.Say("当前不能执行动作");
                return false;
            }
            
            
            var weapon = _owner.Equipment.GetItem(EquipSlot.Weapon);
            if (weapon == null || weapon.Definition.Type != ItemType.Weapon) 
            {
                _owner.Say("需要装备马鞭才能训练坐骑");
                return false;
            }
            
            
            int targetX = _owner.X;
            int targetY = _owner.Y;
            
            switch (dir)
            {
                case 0: targetY--; break; 
                case 1: targetX++; targetY--; break; 
                case 2: targetX++; break; 
                case 3: targetX++; targetY++; break; 
                case 4: targetY++; break; 
                case 5: targetX--; targetY++; break; 
                case 6: targetX--; break; 
                case 7: targetX--; targetY--; break; 
            }
            
            
            if (_owner.CurrentMap == null)
                return false;
                
            var horse = _owner.CurrentMap.GetObjectAt(targetX, targetY) as MonsterEx;
            if (horse == null)
            {
                _owner.Say("目标位置没有马匹");
                return false;
            }
            
            
            
            
            var desc = horse.GetDesc();
            if (desc == null)
            {
                _owner.Say("这匹马不能训练");
                return false;
            }
            
            
            
            if (!desc.Base.ViewName.Contains("马"))
            {
                _owner.Say("这不是骑乘类型的马匹");
                return false;
            }
            
            
            _owner.Say("训练成功！");
            
            
            SetHorse(horse);
            
            
            SendTrainHorseSuccess();
            return true;
        }
        
        
        
        
        private void SendTrainHorseSuccess()
        {
            var builder = new PacketBuilder();
            builder.WriteUInt32(_owner.ObjectId);
            builder.WriteUInt16(0x28F); 
            builder.WriteUInt16(0);
            builder.WriteUInt16(0);
            builder.WriteUInt16(0);
            
            _owner.SendMessage(builder.Build());
        }
        
        
        
        
        public void ToggleHorseRest()
        {
            _horseRest = !_horseRest;
            _owner.Say(_horseRest ? "坐骑休息" : "坐骑工作");
        }
        
        
        
        
        public bool IsHorseRest() => _horseRest;
    }
    
    
    
    
    public class PKSystem
    {
        private readonly HumanPlayer _owner;
        private uint _pkValue;
        private DateTime _lastPkTime;
        private bool _justPk;
        private bool _isSelfDefense; 
        private DateTime _lastSelfDefenseTime;
        
        
        private const uint PK_VALUE_PURPLE = 10;  
        private const uint PK_VALUE_ORANGE = 50;  
        private const uint PK_VALUE_RED = 100;    
        
        
        private const int PK_DECAY_MINUTES = 5;
        
        public PKSystem(HumanPlayer owner)
        {
            _owner = owner;
            _pkValue = 0;
            _lastPkTime = DateTime.MinValue;
            _justPk = false;
            _isSelfDefense = false;
            _lastSelfDefenseTime = DateTime.MinValue;
        }
        
        
        
        
        public uint GetPkValue() => _pkValue;
        
        
        
        
        public void SetPkValue(uint value)
        {
            _pkValue = value;
            UpdateNameColor();
        }
        
        
        
        
        public void AddPkPoint(uint points = 1, bool isSelfDefense = false)
        {
            
            if (isSelfDefense)
            {
                _isSelfDefense = true;
                _lastSelfDefenseTime = DateTime.Now;
                return;
            }
            
            _pkValue += points;
            _lastPkTime = DateTime.Now;
            _justPk = true;
            
            
            UpdateNameColor();
            
            
            CheckWeaponCurse();
            
            
            SendPkValueChanged();
            
            _owner.Say($"PK值增加 {points}，当前PK值: {_pkValue}");
        }
        
        
        
        
        public void DecPkPoint(uint points = 1)
        {
            if (_pkValue >= points)
            {
                _pkValue -= points;
            }
            else
            {
                _pkValue = 0;
            }
            
            UpdateNameColor();
            SendPkValueChanged();
        }
        
        
        
        
        public byte GetNameColor(MapObject? viewer = null)
        {
            
            
            
            
            
            
            
            
            
            if (_pkValue >= PK_VALUE_RED) return 2; 
            if (_pkValue >= PK_VALUE_ORANGE) return 6; 
            if (_pkValue >= PK_VALUE_PURPLE) return 5; 
            
            
            if (_owner.GroupId != 0 && viewer is HumanPlayer viewerPlayer && viewerPlayer.GroupId == _owner.GroupId)
                return 1; 
                
            
            if (_owner.Guild != null && viewer is HumanPlayer viewerPlayer2 && viewerPlayer2.Guild == _owner.Guild)
                return 4; 
                
            return 0; 
        }
        
        
        
        
        public bool CheckPk(AliveObject target)
        {
            if (target is HumanPlayer targetPlayer)
            {
                
                if (targetPlayer.PKSystem._justPk || targetPlayer.PKSystem._isSelfDefense)
                {
                    
                    AddPkPoint(1, true);
                    return true;
                }
                
                
                if (_owner.GroupId != 0 && _owner.GroupId == targetPlayer.GroupId)
                {
                    _owner.Say("不能攻击队友");
                    return false;
                }
                
                
                if (_owner.Guild != null && _owner.Guild == targetPlayer.Guild)
                {
                    _owner.Say("不能攻击同公会成员");
                    return false;
                }
                
                
                AddPkPoint();
                return true;
            }
            
            return false;
        }
        
        
        
        
        private void CheckWeaponCurse()
        {
            
            if (_pkValue >= PK_VALUE_RED)
            {
                
                var weapon = _owner.Equipment.GetItem(EquipSlot.Weapon);
                if (weapon != null)
                {
                    
                    int curseProbability = 30; 
                    
                    
                    if (_pkValue >= PK_VALUE_RED * 2)
                        curseProbability = 50;
                    else if (_pkValue >= PK_VALUE_RED * 3)
                        curseProbability = 70;
                    
                    if (Random.Shared.Next(100) < curseProbability)
                    {
                        
                        CurseWeapon(weapon);
                    }
                }
            }
        }
        
        
        
        
        private void CurseWeapon(ItemInstance weapon)
        {
            if (weapon == null)
                return;
                
            
            
            
            
            int curseValue = weapon.ExtraStats.GetValueOrDefault("Curse", 0);
            curseValue++;
            weapon.ExtraStats["Curse"] = curseValue;
            
            
            int luckyValue = weapon.Definition.Lucky;
            if (luckyValue > 0)
                weapon.Definition.Lucky = luckyValue - 1;
            
            
            _owner.Say("你的武器被诅咒了！");
            
            
            SendWeaponCursed(weapon);
            
            
            Console.WriteLine($"{_owner.Name} 的武器被诅咒，当前诅咒值: {curseValue}");
        }
        
        
        
        
        private void SendWeaponCursed(ItemInstance weapon)
        {
            var builder = new PacketBuilder();
            builder.WriteUInt32(_owner.ObjectId);
            builder.WriteUInt16(0x290); 
            builder.WriteUInt16(0);
            builder.WriteUInt16(0);
            builder.WriteUInt16(0);
            builder.WriteUInt64((ulong)weapon.InstanceId);
            builder.WriteUInt16((ushort)weapon.ExtraStats.GetValueOrDefault("Curse", 0));
            builder.WriteUInt16((ushort)weapon.Definition.Lucky);
            
            _owner.SendMessage(builder.Build());
        }
        
        
        
        
        public List<ItemInstance> GetDeathDropItems()
        {
            var dropItems = new List<ItemInstance>();
            
            
            if (_pkValue >= PK_VALUE_RED)
            {
                
                
                foreach (var slot in Enum.GetValues<EquipSlot>())
                {
                    var item = _owner.Equipment.GetItem(slot);
                    if (item != null && Random.Shared.Next(100) < 50) 
                    {
                        dropItems.Add(item);
                    }
                }
                
                
                var inventoryItems = _owner.Inventory.GetAllItems();
                foreach (var item in inventoryItems.Values)
                {
                    if (Random.Shared.Next(100) < 30) 
                    {
                        dropItems.Add(item);
                    }
                }
            }
            else if (_pkValue >= PK_VALUE_ORANGE)
            {
                
                var inventoryItems = _owner.Inventory.GetAllItems();
                int dropCount = Math.Min(3, inventoryItems.Count);
                for (int i = 0; i < dropCount; i++)
                {
                    if (inventoryItems.Count > 0)
                    {
                        var randomIndex = Random.Shared.Next(inventoryItems.Count);
                        dropItems.Add(inventoryItems.Values.ElementAt(randomIndex));
                    }
                }
            }
            else if (_pkValue >= PK_VALUE_PURPLE)
            {
                
                var inventoryItems = _owner.Inventory.GetAllItems();
                if (inventoryItems.Count > 0)
                {
                    var randomIndex = Random.Shared.Next(inventoryItems.Count);
                    dropItems.Add(inventoryItems.Values.ElementAt(randomIndex));
                }
            }
            
            return dropItems;
        }
        
        
        
        
        private void UpdateNameColor()
        {
            
            var builder = new PacketBuilder();
            builder.WriteUInt32(_owner.ObjectId);
            builder.WriteUInt16(0x285); 
            builder.WriteUInt16(0);
            builder.WriteUInt16(0);
            builder.WriteUInt16(0);
            builder.WriteByte(GetNameColor());
            
            
            var packet = builder.Build();
            _owner.CurrentMap?.SendToNearbyPlayers(_owner.X, _owner.Y, packet);
        }
        
        
        
        
        private void SendPkValueChanged()
        {
            var builder = new PacketBuilder();
            builder.WriteUInt32(_owner.ObjectId);
            builder.WriteUInt16(0x286); 
            builder.WriteUInt16(0);
            builder.WriteUInt16(0);
            builder.WriteUInt16(0);
            builder.WriteUInt32(_pkValue);
            
            _owner.SendMessage(builder.Build());
        }
        
        
        
        
        public void SetJustPk(bool justPk = true)
        {
            _justPk = justPk;
        }
        
        
        
        
        public bool IsSelfDefense()
        {
            return _isSelfDefense && (DateTime.Now - _lastSelfDefenseTime).TotalMinutes < 5;
        }
        
        
        
        
        public void Update()
        {
            
            if (_pkValue > 0 && (DateTime.Now - _lastPkTime).TotalMinutes >= PK_DECAY_MINUTES)
            {
                DecPkPoint(1);
                _lastPkTime = DateTime.Now;
            }
            
            
            if (_justPk && (DateTime.Now - _lastPkTime).TotalSeconds >= 30)
            {
                _justPk = false;
            }
            
            
            if (_isSelfDefense && (DateTime.Now - _lastSelfDefenseTime).TotalMinutes >= 5)
            {
                _isSelfDefense = false;
            }
        }
        
        
        
        
        public string GetPkStatus()
        {
            if (_pkValue >= PK_VALUE_RED) return "红名（罪恶滔天）";
            if (_pkValue >= PK_VALUE_ORANGE) return "橙名（恶贯满盈）";
            if (_pkValue >= PK_VALUE_PURPLE) return "紫名（小有恶名）";
            return "白名（善良公民）";
        }
        
        
        
        
        public bool CanAttack(AliveObject target)
        {
            if (target is HumanPlayer targetPlayer)
            {
                
                if (_owner.GroupId != 0 && _owner.GroupId == targetPlayer.GroupId)
                    return false;
                    
                
                if (_owner.Guild != null && _owner.Guild == targetPlayer.Guild)
                    return false;
                    
                
                if (targetPlayer.Level < 10 && _owner.Level >= 10)
                {
                    _owner.Say("不能攻击新手玩家");
                    return false;
                }
                
                return true;
            }
            
            return true; 
        }
    }
    
    
    
    
    public class AchievementSystem
    {
        private readonly HumanPlayer _owner;
        private readonly Dictionary<uint, Achievement> _achievements = new();
        private readonly Dictionary<AchievementType, uint> _progress = new();
        
        public AchievementSystem(HumanPlayer owner)
        {
            _owner = owner;
            InitializeAchievements();
        }
        
        
        
        
        private void InitializeAchievements()
        {
            
            AddAchievement(new Achievement
            {
                Id = 1,
                Name = "初出茅庐",
                Description = "达到10级",
                Type = AchievementType.Level,
                TargetValue = 10,
                RewardExp = 1000,
                RewardGold = 1000
            });
            
            AddAchievement(new Achievement
            {
                Id = 2,
                Name = "小有所成",
                Description = "达到30级",
                Type = AchievementType.Level,
                TargetValue = 30,
                RewardExp = 5000,
                RewardGold = 5000
            });
            
            
            AddAchievement(new Achievement
            {
                Id = 101,
                Name = "怪物猎人",
                Description = "击杀100只怪物",
                Type = AchievementType.KillMonster,
                TargetValue = 100,
                RewardExp = 2000,
                RewardGold = 2000
            });
            
            
            AddAchievement(new Achievement
            {
                Id = 201,
                Name = "装备收集者",
                Description = "获得10件装备",
                Type = AchievementType.GetItem,
                TargetValue = 10,
                RewardExp = 1500,
                RewardGold = 1500
            });
        }
        
        
        
        
        private void AddAchievement(Achievement achievement)
        {
            _achievements[achievement.Id] = achievement;
        }
        
        
        
        
        public void UpdateProgress(AchievementType type, uint value = 1)
        {
            if (!_progress.ContainsKey(type))
            {
                _progress[type] = 0;
            }
            
            _progress[type] += value;
            CheckAchievements(type);
        }
        
        
        
        
        private void CheckAchievements(AchievementType type)
        {
            var currentValue = _progress.ContainsKey(type) ? _progress[type] : 0;
            
            foreach (var achievement in _achievements.Values)
            {
                if (achievement.Type == type && !achievement.Completed && currentValue >= achievement.TargetValue)
                {
                    CompleteAchievement(achievement.Id);
                }
            }
        }
        
        
        
        
        public bool CompleteAchievement(uint achievementId)
        {
            if (!_achievements.TryGetValue(achievementId, out var achievement) || achievement.Completed)
                return false;
            
            achievement.Completed = true;
            achievement.CompletedTime = DateTime.Now;
            
            
            _owner.AddExp(achievement.RewardExp);
            _owner.AddGold(achievement.RewardGold);
            
            _owner.Say($"成就达成: {achievement.Name} - {achievement.Description}");
            _owner.Say($"获得奖励: {achievement.RewardExp}经验, {achievement.RewardGold}金币");
            
            
            return true;
        }
        
        
        
        
        public List<Achievement> GetAchievements()
        {
            return _achievements.Values.ToList();
        }
        
        
        
        
        public uint GetProgress(AchievementType type)
        {
            return _progress.TryGetValue(type, out var value) ? value : 0;
        }
    }
    
    
    
    
    public class MailSystem
    {
        private readonly HumanPlayer _owner;
        private readonly List<Mail> _mails = new();
        private readonly object _mailLock = new();
        
        public MailSystem(HumanPlayer owner)
        {
            _owner = owner;
        }
        
        
        
        
        public bool SendMail(string receiverName, string title, string content, List<ItemInstance>? attachments = null)
        {
            if (string.IsNullOrEmpty(receiverName) || string.IsNullOrEmpty(title))
            {
                _owner.Say("收件人或标题不能为空");
                return false;
            }
            
            
            var receiver = HumanPlayerMgr.Instance.FindByName(receiverName);
            if (receiver == null)
            {
                _owner.Say($"玩家 {receiverName} 不存在或不在线");
                return false;
            }
            
            
            if (attachments != null && attachments.Count > 0)
            {
                
                if (attachments.Count > 5)
                {
                    _owner.Say("附件数量不能超过5个");
                    return false;
                }
                
                
                foreach (var attachment in attachments)
                {
                    if (!_owner.Inventory.HasItem((ulong)attachment.InstanceId))
                    {
                        _owner.Say("附件物品不属于你");
                        return false;
                    }
                }
            }
            
            
            var mail = new Mail
            {
                Id = GenerateMailId(),
                Sender = _owner.Name,
                Receiver = receiverName,
                Title = title,
                Content = content,
                SendTime = DateTime.Now,
                IsRead = false,
                Attachments = attachments,
                AttachmentsClaimed = false
            };
            
            
            if (!SaveMailToDatabase(mail))
            {
                _owner.Say("邮件发送失败，数据库错误");
                return false;
            }
            
            
            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    _owner.Inventory.RemoveItem((ulong)attachment.InstanceId, 1);
                }
            }
            
            
            receiver.MailSystem.ReceiveMail(mail);
            
            
            _owner.Say($"邮件已发送给 {receiverName}");
            
            
            Console.WriteLine($"{_owner.Name} 发送邮件给 {receiverName}，标题: {title}");
            
            return true;
        }
        
        
        
        
        private uint GenerateMailId()
        {
            
            return (uint)DateTime.Now.Ticks;
        }
        
        
        
        
        private bool SaveMailToDatabase(Mail mail)
        {
            
            
            return true;
        }
        
        
        
        
        public void ReceiveMail(Mail mail)
        {
            lock (_mailLock)
            {
                _mails.Add(mail);
            }
            
            
            _owner.Say("你有新邮件");
        }
        
        
        
        
        public List<Mail> GetMails()
        {
            lock (_mailLock)
            {
                return new List<Mail>(_mails);
            }
        }
        
        
        
        
        public Mail? ReadMail(uint mailId)
        {
            lock (_mailLock)
            {
                var mail = _mails.FirstOrDefault(m => m.Id == mailId);
                if (mail != null && !mail.IsRead)
                {
                    mail.IsRead = true;
                    mail.ReadTime = DateTime.Now;
                }
                return mail;
            }
        }
        
        
        
        
        public bool DeleteMail(uint mailId)
        {
            lock (_mailLock)
            {
                var mail = _mails.FirstOrDefault(m => m.Id == mailId);
                if (mail == null)
                    return false;
                    
                _mails.Remove(mail);
                return true;
            }
        }
        
        
        
        
        public bool ClaimAttachment(uint mailId)
        {
            lock (_mailLock)
            {
                var mail = _mails.FirstOrDefault(m => m.Id == mailId);
                if (mail == null || mail.Attachments == null || mail.Attachments.Count == 0)
                    return false;
                    
                if (mail.AttachmentsClaimed)
                {
                    _owner.Say("附件已领取");
                    return false;
                }
                
                
                foreach (var item in mail.Attachments)
                {
                    if (!_owner.Inventory.AddItem(item))
                    {
                        _owner.Say("背包空间不足");
                        return false;
                    }
                }
                
                mail.AttachmentsClaimed = true;
                mail.ClaimTime = DateTime.Now;
                _owner.Say("附件领取成功");
                return true;
            }
        }
    }
    
    
    
    
    public enum AchievementType
    {
        Level,
        KillMonster,
        GetItem,
        CompleteQuest,
        JoinGuild,
        PvPKill,
        UseSkill,
        CraftItem,
        SignIn
    }
    
    
    
    
    public class Achievement
    {
        public uint Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public AchievementType Type { get; set; }
        public uint TargetValue { get; set; }
        public uint RewardExp { get; set; }
        public uint RewardGold { get; set; }
        public bool Completed { get; set; }
        public DateTime? CompletedTime { get; set; }
    }
    
    
    
    
    public class Mail
    {
        public uint Id { get; set; }
        public string Sender { get; set; } = string.Empty;
        public string Receiver { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime SendTime { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadTime { get; set; }
        public List<ItemInstance>? Attachments { get; set; }
        public bool AttachmentsClaimed { get; set; }
        public DateTime? ClaimTime { get; set; }
    }

    #region 每日签到系统

    /// <summary>
    /// 每日签到系统
    /// - 连续签到递增奖励（7天一轮回）
    /// - 断签重置连续天数
    /// - 奖励：金币/经验/物品
    /// - 存储：JSON文件（每个角色独立文件）
    /// </summary>
    public class DailySignInSystem
    {
        private readonly HumanPlayer _owner;

        // 签到状态（内存）
        private DateTime _lastSignInDate;         // 上次签到日期
        private int _consecutiveDays;              // 连续签到天数
        private bool _todaySigned;                // 今日是否已签到
        private uint _totalSignInDays;            // 累计签到天数

        // JSON存储路径
        private static readonly string SignInDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "SignIn");
        private string SignInFilePath => Path.Combine(SignInDataDir, $"{_owner.CharDBId}.json");

        // 7天签到奖励配置（按连续天数索引，1-7天）
        private static readonly DailyReward[] DailyRewards = new[]
        {
            new DailyReward { Day = 1,  Gold = 1000,   Exp = 5000,   ItemId = 0,   ItemCount = 0,  Desc = "1天奖励" },
            new DailyReward { Day = 2,  Gold = 2000,   Exp = 10000,  ItemId = 0,   ItemCount = 0,  Desc = "2天奖励" },
            new DailyReward { Day = 3,  Gold = 3000,   Exp = 15000,  ItemId = 0,   ItemCount = 0,  Desc = "3天奖励" },
            new DailyReward { Day = 4,  Gold = 5000,   Exp = 25000,  ItemId = 0,   ItemCount = 0,  Desc = "4天奖励" },
            new DailyReward { Day = 5,  Gold = 8000,   Exp = 40000,  ItemId = 0,   ItemCount = 0,  Desc = "5天奖励" },
            new DailyReward { Day = 6,  Gold = 12000,  Exp = 60000,  ItemId = 0,   ItemCount = 0,  Desc = "6天奖励" },
            new DailyReward { Day = 7,  Gold = 20000,  Exp = 100000, ItemId = 42001, ItemCount = 1, Desc = "7天大奖" }, // 祝福油
        };

        public DailySignInSystem(HumanPlayer owner)
        {
            _owner = owner;
            _lastSignInDate = DateTime.MinValue;
            _consecutiveDays = 0;
            _todaySigned = false;
            _totalSignInDays = 0;
        }

        /// <summary>
        /// 初始化时加载签到数据
        /// </summary>
        public void Load()
        {
            try
            {
                if (!File.Exists(SignInFilePath))
                    return;

                var json = File.ReadAllText(SignInFilePath, System.Text.Encoding.UTF8);
                var data = System.Text.Json.JsonSerializer.Deserialize<SignInData>(json);
                if (data == null) return;

                _consecutiveDays = data.ConsecutiveDays;
                _totalSignInDays = data.TotalDays;
                _lastSignInDate = data.LastSignInDate;
                _todaySigned = IsToday(_lastSignInDate);
            }
            catch (Exception ex)
            {
                LogManager.Default.Error($"加载签到数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存签到数据到文件
        /// </summary>
        private void Save()
        {
            try
            {
                if (!Directory.Exists(SignInDataDir))
                    Directory.CreateDirectory(SignInDataDir);

                var data = new SignInData
                {
                    CharId = _owner.CharDBId,
                    ConsecutiveDays = _consecutiveDays,
                    TotalDays = _totalSignInDays,
                    LastSignInDate = _lastSignInDate
                };

                var json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(SignInFilePath, json, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                LogManager.Default.Error($"保存签到数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行签到
        /// </summary>
        public bool SignIn()
        {
            var now = DateTime.Today;

            if (_todaySigned)
            {
                _owner.Say("今日已签到，明天再来吧！");
                return false;
            }

            // 检查是否连续（昨天签到过）
            var yesterday = now.AddDays(-1);
            bool isConsecutive = IsSameDay(_lastSignInDate, yesterday);

            if (isConsecutive)
            {
                _consecutiveDays++;
            }
            else
            {
                // 断签，重置连续天数
                _consecutiveDays = 1;
            }

            _todaySigned = true;
            _lastSignInDate = now;
            _totalSignInDays++;

            // 获取当前连续天数的奖励（1-7循环）
            int rewardDay = ((_consecutiveDays - 1) % 7) + 1;
            var reward = DailyRewards[rewardDay - 1];

            // 发放奖励
            _owner.AddGold(reward.Gold);
            _owner.AddExp(reward.Exp);

            string rewardMsg = $"获得奖励：{reward.Gold}金币、{reward.Exp}经验";
            if (reward.ItemId > 0 && reward.ItemCount > 0)
            {
                var itemDef = ItemManager.Instance.GetDefinition(reward.ItemId);
                if (itemDef != null)
                {
                    var item = new ItemInstance(itemDef, (long)ItemManager.Instance.AllocateTempMakeIndex());
                    item.Count = reward.ItemCount;
                    if (_owner.Inventory.AddItem(item))
                    {
                        rewardMsg += $"、{itemDef.Name} x{reward.ItemCount}";
                    }
                }
            }

            _owner.Say($"=== 签到成功！第{_consecutiveDays}天 ===");
            _owner.Say(rewardMsg);
            _owner.Say($"累计签到：{_totalSignInDays}天");
            LogManager.Default.Info($"玩家 {_owner.Name} 签到成功：第{_consecutiveDays}天，累计{_totalSignInDays}天");

            // 触发成就更新
            _owner.AchievementSystem?.UpdateProgress(AchievementType.SignIn, 1);

            // 保存
            Save();

            return true;
        }

        /// <summary>
        /// 获取签到状态信息（用于客户端显示）
        /// </summary>
        public SignInStatusInfo GetStatus()
        {
            int rewardDay = (_todaySigned ? _consecutiveDays : (_consecutiveDays == 0 ? 0 : _consecutiveDays)) % 7;
            if (rewardDay == 0) rewardDay = 7;

            return new SignInStatusInfo
            {
                TodaySigned = _todaySigned,
                ConsecutiveDays = _consecutiveDays,
                TotalDays = _totalSignInDays,
                CurrentRewardDay = rewardDay
            };
        }

        private static bool IsToday(DateTime date)
        {
            return IsSameDay(date, DateTime.Today);
        }

        private static bool IsSameDay(DateTime a, DateTime b)
        {
            return a.Year == b.Year && a.Month == b.Month && a.Day == b.Day;
        }
    }

    #endregion

    /// <summary>
    /// 签到数据（JSON序列化用）
    /// </summary>
    internal class SignInData
    {
        public uint CharId { get; set; }
        public int ConsecutiveDays { get; set; }
        public uint TotalDays { get; set; }
        public DateTime LastSignInDate { get; set; }
    }

    /// <summary>
    /// 单日签到奖励配置
    /// </summary>
    public class DailyReward
    {
        public int Day { get; set; }
        public uint Gold { get; set; }
        public uint Exp { get; set; }
        public int ItemId { get; set; }
        public int ItemCount { get; set; }
        public string Desc { get; set; } = "";
    }

    /// <summary>
    /// 签到状态信息（用于客户端显示）
    /// </summary>
    public class SignInStatusInfo
    {
        public bool TodaySigned { get; set; }
        public int ConsecutiveDays { get; set; }
        public uint TotalDays { get; set; }
        public int CurrentRewardDay { get; set; }
    }

    #region 在线挂机系统

    /// <summary>
    /// 在线挂机系统
    /// - 自动寻怪/攻击/拾取
    /// - 自动使用药水/技能
    /// - 挂机范围和安全区检测
    /// - 与客户端解耦，纯服务端AI
    /// </summary>
    public class OnlineTrainingSystem
    {
        private readonly HumanPlayer _owner;

        // 挂机状态
        private bool _isEnabled;
        private DateTime _lastUpdateTime;
        private DateTime _lastPickupTime;
        private DateTime _lastHealTime;
        private DateTime _lastBuffTime;
        private DateTime _lastMoveTime;

        // 挂机配置
        private int _searchRange = 8;       // 搜索怪物范围
        private int _pickupRange = 2;        // 拾取物品范围
        private int _healHpPercent = 50;     // 低于此血量百分比时喝药
        private int _healMpPercent = 30;     // 低于此魔法百分比时喝药
        private int _safeHpPercent = 80;     // 低于此血量时停止攻击
        private int _autoSkillSlot = 0;      // 自动释放技能槽位（-1=不用技能）

        // 挂机状态
        private Monster? _currentTarget;
        private int _targetDeadCount;        // 连续未找到目标计数
        private int _stuckCount;             // 卡住计数

        // 坐标记录（用于检测卡住）
        private ushort _lastPosX;
        private ushort _lastPosY;
        private DateTime _lastPosCheckTime;

        public bool IsEnabled => _isEnabled;

        public OnlineTrainingSystem(HumanPlayer owner)
        {
            _owner = owner;
            _isEnabled = false;
            _lastUpdateTime = DateTime.Now;
        }

        /// <summary>
        /// 开始挂机
        /// </summary>
        public void Start()
        {
            if (_owner.IsDead)
            {
                _owner.Say("死亡状态下无法挂机！");
                return;
            }

            _isEnabled = true;
            _currentTarget = null;
            _targetDeadCount = 0;
            _stuckCount = 0;
            _lastPosX = _owner.X;
            _lastPosY = _owner.Y;
            _lastPosCheckTime = DateTime.Now;
            _lastUpdateTime = DateTime.Now;

            _owner.Say("=== 开始挂机 ===");
            LogManager.Default.Info($"玩家 {_owner.Name} 开始挂机");
        }

        /// <summary>
        /// 停止挂机
        /// </summary>
        public void Stop()
        {
            if (!_isEnabled) return;

            _isEnabled = false;
            _currentTarget = null;
            _owner.Say("=== 挂机已停止 ===");
            LogManager.Default.Info($"玩家 {_owner.Name} 停止挂机");
        }

        /// <summary>
        /// 每帧更新（由 HumanPlayer.Update 调用）
        /// </summary>
        public void Update()
        {
            if (!_isEnabled) return;
            if (_owner.IsDead)
            {
                Stop();
                return;
            }

            var now = DateTime.Now;

            // 挂机逻辑每 200ms 执行一次
            if ((now - _lastUpdateTime).TotalMilliseconds < 200)
                return;
            _lastUpdateTime = now;

            // 1. 检测是否卡住
            CheckStuck();

            // 2. 安全检测
            if (!CheckSafety())
                return;

            // 3. 检查是否需要补血/补魔
            if (CheckAndUsePotion())
                return;

            // 4. 检查是否有物品可拾取
            if (CheckAndPickupItem())
                return;

            // 5. 攻击逻辑
            UpdateCombat();
        }

        /// <summary>
        /// 检测是否卡住（坐标没变超过5秒）
        /// </summary>
        private void CheckStuck()
        {
            var now = DateTime.Now;
            if ((now - _lastPosCheckTime).TotalSeconds >= 5)
            {
                if (_owner.X == _lastPosX && _owner.Y == _lastPosY)
                {
                    _stuckCount++;
                    if (_stuckCount >= 3)
                    {
                        // 随机移动一步
                        var dirs = Enum.GetValues<Direction>();
                        var randomDir = dirs[Random.Shared.Next(dirs.Length)];
                        _owner.Walk(randomDir);
                        _stuckCount = 0;
                    }
                }
                else
                {
                    _stuckCount = 0;
                }
                _lastPosX = _owner.X;
                _lastPosY = _owner.Y;
                _lastPosCheckTime = now;
            }
        }

        /// <summary>
        /// 安全检测（血量过低或安全区）
        /// </summary>
        private bool CheckSafety()
        {
            if (_owner.CurrentHP <= 0 || _owner.IsDead)
            {
                Stop();
                return false;
            }

            int hpPercent = _owner.MaxHP > 0 ? (_owner.CurrentHP * 100 / _owner.MaxHP) : 100;

            // 血量低于安全线，停止攻击尝试移动
            if (hpPercent < _safeHpPercent)
            {
                // 尝试随机移动找安全的地方
                var dirs = Enum.GetValues<Direction>();
                for (int i = 0; i < 3; i++)
                {
                    var dir = dirs[Random.Shared.Next(dirs.Length)];
                    if (_owner.Walk(dir))
                        return false;
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// 检查并使用药水
        /// </summary>
        private bool CheckAndUsePotion()
        {
            var now = DateTime.Now;
            if ((now - _lastHealTime).TotalSeconds < 2)
                return false;

            int hpPercent = _owner.MaxHP > 0 ? (_owner.CurrentHP * 100 / _owner.MaxHP) : 100;
            int mpPercent = _owner.MaxMP > 0 ? (_owner.CurrentMP * 100 / _owner.MaxMP) : 100;

            // 先检查HP
            if (hpPercent < _healHpPercent)
            {
                // 找背包里的HP药水
                var hpPotion = FindPotionItem(0); // 0=太阳水类型，实际按道具定义
                if (hpPotion != null)
                {
                    int slot = _owner.Inventory.FindSlotByMakeIndex(hpPotion.GetMakeIndex());
                    if (slot >= 0)
                        _owner.UseItem(slot);
                    _lastHealTime = now;
                    return true;
                }
            }

            // 再检查MP
            if (mpPercent < _healMpPercent)
            {
                var mpPotion = FindMpPotionItem();
                if (mpPotion != null)
                {
                    int slot = _owner.Inventory.FindSlotByMakeIndex(mpPotion.GetMakeIndex());
                    if (slot >= 0)
                        _owner.UseItem(slot);
                    _lastHealTime = now;
                    return true;
                }
            }

            return false;
        }

        private ItemInstance? FindPotionItem(int potionType)
        {
            foreach (var item in _owner.Inventory.GetAllItems().Values)
            {
                if (item == null) continue;
                // 简单判断：ItemId 在某阈值范围的是HP药水
                if (item.ItemId >= 20000 && item.ItemId <= 20999)
                    return item;
            }
            return null;
        }

        private ItemInstance? FindMpPotionItem()
        {
            foreach (var item in _owner.Inventory.GetAllItems().Values)
            {
                if (item == null) continue;
                // 魔法药水
                if (item.ItemId >= 21000 && item.ItemId <= 21999)
                    return item;
            }
            return null;
        }

        /// <summary>
        /// 检查并拾取地面物品
        /// </summary>
        private bool CheckAndPickupItem()
        {
            var now = DateTime.Now;
            if ((now - _lastPickupTime).TotalMilliseconds < 500)
                return false;

            var map = _owner.CurrentMap as LogicMap;
            if (map == null) return false;

            var nearbyItems = map.GetItemsInRange(_owner.X, _owner.Y, _pickupRange);
            foreach (var mapItem in nearbyItems)
            {
                if (mapItem == null) continue;
                // 拾取
                if (PickupItem(mapItem))
                {
                    _lastPickupTime = now;
                    return true;
                }
            }

            return false;
        }

        private bool PickupItem(MapItem mapItem)
        {
            // 检查背包是否有空间
            if (_owner.Inventory.GetUsedSlots() >= _owner.Inventory.MaxSlots)
            {
                return false;
            }

            // 发送拾取消息
            var builder = new PacketBuilder();
            builder.WriteUInt32(mapItem.ObjectId);
            builder.WriteUInt16(0);
            builder.WriteUInt16(0);
            builder.WriteUInt16(0);
            byte[] packet = builder.Build();

            // 使用GM命令或直接处理
            // 实际上拾取需要客户端发起，这里模拟
            _owner.SendMessage(packet);
            return true;
        }

        /// <summary>
        /// 更新战斗逻辑
        /// </summary>
        private void UpdateCombat()
        {
            var now = DateTime.Now;

            // 如果当前正在攻击动作中，不做处理
            if (_owner.CurrentAction != ActionType.None)
                return;

            // 检查当前目标是否有效
            if (_currentTarget != null)
            {
                if (_currentTarget.IsDead || _currentTarget.CurrentMap != _owner.CurrentMap)
                {
                    _currentTarget = null;
                    _targetDeadCount++;
                }
            }

            // 如果没有目标，搜索怪物
            if (_currentTarget == null)
            {
                _currentTarget = FindMonster();
                _targetDeadCount = 0;
            }

            // 多次没找到目标，随机走动
            if (_currentTarget == null)
            {
                _targetDeadCount++;
                if (_targetDeadCount >= 3 && (now - _lastMoveTime).TotalSeconds >= 3)
                {
                    var dirs = Enum.GetValues<Direction>();
                    var dir = dirs[Random.Shared.Next(dirs.Length)];
                    _owner.Walk(dir);
                    _lastMoveTime = now;
                    _targetDeadCount = 0;
                }
                return;
            }

            // 计算距离
            int dx = Math.Abs(_currentTarget.X - _owner.X);
            int dy = Math.Abs(_currentTarget.Y - _owner.Y);
            int distance = Math.Max(dx, dy);

            // 远程职业可以更远攻击
            int attackRange = _owner.Job == 2 ? 8 : 1; // 法师职业

            if (distance > attackRange)
            {
                // 走向目标
                MoveToward(_currentTarget.X, _currentTarget.Y);
            }
            else
            {
                // 攻击
                AttackTarget();
            }
        }

        private Monster? FindMonster()
        {
            var map = _owner.CurrentMap as LogicMap;
            if (map == null) return null;

            var monsters = map.GetMonstersInRange(_owner.X, _owner.Y, _searchRange);
            Monster? nearest = null;
            int minDist = int.MaxValue;

            foreach (var m in monsters)
            {
                if (m is not Monster monster)
                    continue;
                if (monster.IsDead)
                    continue;

                int dx = Math.Abs(monster.X - _owner.X);
                int dy = Math.Abs(monster.Y - _owner.Y);
                int dist = dx + dy;

                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = monster;
                }
            }

            return nearest;
        }

        private void MoveToward(ushort targetX, ushort targetY)
        {
            // 计算方向
            Direction dir;
            int dx = targetX - _owner.X;
            int dy = targetY - _owner.Y;

            if (dx == 0 && dy == 0) return;

            // 简单方向判断
            if (Math.Abs(dx) > Math.Abs(dy))
            {
                dir = dx > 0 ? Direction.Right : Direction.Left;
            }
            else
            {
                dir = dy > 0 ? Direction.Down : Direction.Up;
            }

            // 优先走向目标
            if (_owner.Walk(dir))
                return;

            // 尝试相邻方向
            var dirs = new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
            foreach (var d in dirs)
            {
                if (d == dir) continue;
                if (_owner.Walk(d))
                    return;
            }
        }

        private void AttackTarget()
        {
            if (_currentTarget == null || _currentTarget.IsDead)
                return;

            // 计算朝向
            Direction dir = GetDirection(_owner.X, _owner.Y, _currentTarget.X, _currentTarget.Y);

            // 普通攻击
            _owner.Attack(dir);

            // 更新成就击杀数
            _owner.AchievementSystem?.UpdateProgress(AchievementType.PvPKill, 1);
        }

        private static Direction GetDirection(ushort fromX, ushort fromY, ushort toX, ushort toY)
        {
            int dx = toX - fromX;
            int dy = toY - fromY;

            if (dx == 0 && dy == 0) return Direction.Down;
            if (Math.Abs(dx) > Math.Abs(dy))
                return dx > 0 ? Direction.Right : Direction.Left;
            else
                return dy > 0 ? Direction.Down : Direction.Up;
        }
    }
    #endregion

    #region Gem System

    /// <summary>
    /// 宝石镶嵌系统 - 装备打孔、宝石镶嵌/卸下/合成
    /// </summary>
    public sealed class GemInlaySystem
    {
        // 装备可打孔数量上限（按品质）
        private static readonly Dictionary<ItemQuality, int> MaxHolesByQuality = new()
        {
            { ItemQuality.Normal, 0 },      // 白板不可打孔
            { ItemQuality.Fine, 1 },      // 白装1孔
            { ItemQuality.Rare, 2 },      // 绿装2孔
            { ItemQuality.Epic, 3 },       // 蓝装3孔
            { ItemQuality.Legendary, 4 },     // 紫装4孔
            { ItemQuality.Mythic, 5 },     // 橙装5孔
        };

        // 宝石类型枚举
        public enum GemType : byte
        {
            Red = 1,      // 攻击宝石
            Blue = 2,     // 防御宝石
            Green = 3,    // 生命宝石
            Yellow = 4,   // 幸运宝石
            Purple = 5,   // 魔法宝石
            White = 6,    // 经验宝石（打怪经验加成）
        }

        // 宝石定义
        public static readonly Dictionary<GemType, (int DC, int AC, int HP, int Lucky, int MC, int ExpBonus)> GemStats = new()
        {
            { GemType.Red,    (DC: 5, AC: 0, HP: 0, Lucky: 0, MC: 0, ExpBonus: 0) },    // +5攻击
            { GemType.Blue,   (DC: 0, AC: 5, HP: 0, Lucky: 0, MC: 0, ExpBonus: 0) },   // +5防御
            { GemType.Green,  (DC: 0, AC: 0, HP: 100, Lucky: 0, MC: 0, ExpBonus: 0) },// +100生命
            { GemType.Yellow, (DC: 0, AC: 0, HP: 0, Lucky: 1, MC: 0, ExpBonus: 0) },   // +1幸运
            { GemType.Purple, (DC: 0, AC: 0, HP: 0, Lucky: 0, MC: 5, ExpBonus: 0) },  // +5魔法
            { GemType.White,  (DC: 0, AC: 0, HP: 0, Lucky: 0, MC: 0, ExpBonus: 10) }, // +10%经验
        };

        // 宝石等级缩放（每级+50%效果）
        private static readonly int[] GemLevelScale = { 100, 150, 200, 300, 400, 500 };

        private readonly HumanPlayer _owner;

        public GemInlaySystem(HumanPlayer owner)
        {
            _owner = owner;
        }

        /// <summary>给装备打一个孔（消耗金币）</summary>
        public string PunchHole(ItemInstance equipment)
        {
            if (equipment == null)
                return "请选择要打孔的装备";

            if (equipment.Definition.Type < ItemType.Weapon || equipment.Definition.Type > ItemType.Ring)
                return "此物品无法打孔";

            int currentHoles = GetHoleCount(equipment);
            int maxHoles = MaxHolesByQuality.GetValueOrDefault(equipment.Definition.Quality, 0);

            if (currentHoles >= maxHoles)
                return $"该装备最多可打 {maxHoles} 个孔，当前已有 {currentHoles} 个";

            // 打孔费用：基础2000金币，每孔递增
            uint cost = (uint)(2000 * (currentHoles + 1) * 100);
            if (_owner.Gold < cost)
                return $"打孔需要 {cost} 金币，当前金币不足";

            _owner.Gold -= cost;
            _owner.SendMoneyChanged(MoneyType.Gold);

            int newHoleIndex = currentHoles;
            // 在 ExtraStats 中记录孔位，key = "Hole_0", "Hole_1", ...
            equipment.ExtraStats[$"Hole_{newHoleIndex}"] = 0; // 0表示空孔
            equipment.ExtraStats["HoleCount"] = currentHoles + 1;

            _owner.RecalcTotalStats();
            return $"打孔成功！消耗 {cost} 金币。";
        }

        /// <summary>镶嵌宝石到装备孔位</summary>
        public string InlayGem(ItemInstance equipment, int holeIndex, ItemInstance gemItem)
        {
            if (equipment == null)
                return "请选择要镶嵌的装备";
            if (gemItem == null)
                return "请选择要镶嵌的宝石";

            int holeCount = GetHoleCount(equipment);
            if (holeIndex < 0 || holeIndex >= holeCount)
                return "无效的孔位";

            string holeKey = $"Hole_{holeIndex}";
            if (!equipment.ExtraStats.ContainsKey(holeKey))
                return "该孔位不存在";

            if (equipment.ExtraStats[holeKey] != 0)
                return "该孔位已有宝石，请先取下";

            // 验证宝石是否是合法宝石物品
            if (!TryParseGemType(gemItem.Definition.Name, out GemType gemType))
                return "该物品不是可镶嵌的宝石";

            // 消耗宝石（数量-1，0则删除）
            if (gemItem.Count > 1)
                gemItem.Count--;
            else
            {
                int slot = _owner.Inventory.FindSlotByMakeIndex(gemItem.GetMakeIndex());
                if (slot >= 0)
                    _owner.Inventory.RemoveItem(slot, 1);
            }

            // 镶嵌
            equipment.ExtraStats[holeKey] = (int)gemType;

            _owner.RecalcTotalStats();
            return $"成功镶嵌 {gemItem.Definition.Name}！";
        }

        /// <summary>从装备孔位取下宝石（返还宝石）</summary>
        public string RemoveGem(ItemInstance equipment, int holeIndex)
        {
            if (equipment == null)
                return "请选择要取下宝石的装备";

            int holeCount = GetHoleCount(equipment);
            if (holeIndex < 0 || holeIndex >= holeCount)
                return "无效的孔位";

            string holeKey = $"Hole_{holeIndex}";
            if (!equipment.ExtraStats.ContainsKey(holeKey))
                return "该孔位不存在";

            int gemTypeValue = equipment.ExtraStats[holeKey];
            if (gemTypeValue == 0)
                return "该孔位是空的";

            if (_owner.Inventory.GetUsedSlots() >= _owner.Inventory.MaxSlots)
                return "背包空间不足，无法取下宝石";

            var gemType = (GemType)gemTypeValue;
            string gemName = GetGemName(gemType);

            // 返还宝石给玩家
            var gemDef = ItemManager.Instance.GetDefinitionByName(gemName);
            if (gemDef != null)
            {
                var gemInstance = new ItemInstance(gemDef, (long)ItemManager.Instance.AllocateTempMakeIndex());
                if (gemInstance != null)
                    _owner.Inventory.AddItem(gemInstance);
            }

            // 清空孔位
            equipment.ExtraStats[holeKey] = 0;

            _owner.RecalcTotalStats();
            return $"已取下 {gemName}。";
        }

        /// <summary>获取装备已开孔数量</summary>
        public static int GetHoleCount(ItemInstance equipment)
        {
            if (equipment?.ExtraStats == null)
                return 0;
            return equipment.ExtraStats.GetValueOrDefault("HoleCount", 0);
        }

        /// <summary>获取装备已镶嵌宝石的总属性加成（用于 RecalcTotalStats）</summary>
        public static (int DC, int AC, int HP, int Lucky, int MC, int ExpBonus) GetGemBonus(ItemInstance? equipment)
        {
            int totalDC = 0, totalAC = 0, totalHP = 0, totalLucky = 0, totalMC = 0, totalExpBonus = 0;

            if (equipment?.ExtraStats == null)
                return (totalDC, totalAC, totalHP, totalLucky, totalMC, totalExpBonus);

            int holeCount = equipment.ExtraStats.GetValueOrDefault("HoleCount", 0);
            for (int i = 0; i < holeCount; i++)
            {
                int gemTypeVal = equipment.ExtraStats.GetValueOrDefault($"Hole_{i}", 0);
                if (gemTypeVal > 0 && GemStats.TryGetValue((GemType)gemTypeVal, out var stats))
                {
                    // 宝石等级：默认1级，按特殊属性缩放（这里简化处理，全部按1级）
                    int scale = GemLevelScale[0];
                    totalDC += (stats.DC * scale) / 100;
                    totalAC += (stats.AC * scale) / 100;
                    totalHP += (stats.HP * scale) / 100;
                    totalLucky += (stats.Lucky * scale) / 100;
                    totalMC += (stats.MC * scale) / 100;
                    totalExpBonus += stats.ExpBonus;
                }
            }

            return (totalDC, totalAC, totalHP, totalLucky, totalMC, totalExpBonus);
        }

        /// <summary>宝石名称推断类型</summary>
        private static bool TryParseGemName(string name, out GemType type)
        {
            foreach (var kv in GemStats)
            {
                if (name.Contains(kv.Key.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    type = kv.Key;
                    return true;
                }
            }
            type = default;
            return false;
        }

        /// <summary>根据物品名称尝试解析宝石类型</summary>
        private static bool TryParseGemType(string name, out GemType type)
        {
            // 匹配 "红宝石" / "攻击宝石" / "宝石(红)" 等命名
            string lower = name.ToLower();
            if (lower.Contains("攻击") || lower.Contains("红")) { type = GemType.Red; return true; }
            if (lower.Contains("防御") || lower.Contains("蓝")) { type = GemType.Blue; return true; }
            if (lower.Contains("生命") || lower.Contains("绿")) { type = GemType.Green; return true; }
            if (lower.Contains("幸运") || lower.Contains("黄")) { type = GemType.Yellow; return true; }
            if (lower.Contains("魔法") || lower.Contains("紫")) { type = GemType.Purple; return true; }
            if (lower.Contains("经验") || lower.Contains("白")) { type = GemType.White; return true; }
            type = default;
            return false;
        }

        /// <summary>宝石类型对应的中文名</summary>
        private static string GetGemName(GemType type)
        {
            return type switch
            {
                GemType.Red => "攻击宝石",
                GemType.Blue => "防御宝石",
                GemType.Green => "生命宝石",
                GemType.Yellow => "幸运宝石",
                GemType.Purple => "魔法宝石",
                GemType.White => "经验宝石",
                _ => "未知宝石"
            };
        }
    }

    #region VIP系统

    /// <summary>
    /// VIP会员系统
    /// 8级VIP，每日礼包，经验/爆率/专属特权
    /// 数据存储：JSON文件 Data/Vip/{charId}.json
    /// </summary>
    public class VipSystem
    {
        private readonly HumanPlayer _owner;

        public VipLevel VipLevel { get; private set; }
        public DateTime VipExpireTime { get; private set; }
        public DateTime DateLastGift { get; private set; }
        public int TotalRecharge { get; private set; }  // 累计充值(元)，决定VIP等级

        private static readonly string VipDataPath = Path.Combine("Data", "Vip");

        public VipSystem(HumanPlayer owner)
        {
            _owner = owner;
            VipLevel = VipLevel.None;
            VipExpireTime = DateTime.MinValue;
            DateLastGift = DateTime.MinValue;
            TotalRecharge = 0;
            Load();
        }

        #region 数据加载/保存

        private string GetFilePath() => Path.Combine(VipDataPath, $"{_owner.CharDBId}.json");

        public void Load()
        {
            try
            {
                string path = GetFilePath();
                if (!File.Exists(path)) return;
                var json = File.ReadAllText(path);
                var data = JsonSerializer.Deserialize<VipData>(json);
                if (data == null) return;
                VipLevel = (VipLevel)data.VipLevel;
                VipExpireTime = DateTime.Parse(data.VipExpireTime);
                DateLastGift = DateTime.Parse(data.DateLastGift);
                TotalRecharge = data.TotalRecharge;
            }
            catch { }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(VipDataPath);
                var data = new VipData
                {
                    VipLevel = (int)VipLevel,
                    VipExpireTime = VipExpireTime.ToString("O"),
                    DateLastGift = DateLastGift.ToString("O"),
                    TotalRecharge = TotalRecharge
                };
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(GetFilePath(), json);
            }
            catch { }
        }

        #endregion

        #region VIP等级与特权

        /// <summary>
        /// 根据累计充值提升VIP等级（内部GM用）
        /// </summary>
        public void AddRecharge(int amount)
        {
            if (amount <= 0) return;
            TotalRecharge += amount;
            VipLevel newLevel = CalculateVipLevel(TotalRecharge);
            if (newLevel != VipLevel)
            {
                VipLevel = newLevel;
                VipExpireTime = DateTime.Now.AddYears(1);
                Save();
                _owner.SaySystem($"恭喜！您的VIP等级提升至 {(int)VipLevel}级");
            }
        }

        /// <summary>
        /// 手动设置VIP等级（GM命令用）
        /// </summary>
        public void SetVipLevel(int level, int days = 365)
        {
            VipLevel = (VipLevel)Math.Clamp(level, 0, 8);
            VipExpireTime = DateTime.Now.AddDays(days);
            Save();
            _owner.SaySystem($"VIP等级已设置为 {(int)VipLevel}级，有效期{days}天");
        }

        private static VipLevel CalculateVipLevel(int totalRecharge)
        {
            return totalRecharge switch
            {
                >= 5000 => VipLevel.Gold,    // 5000+ 钻石VIP
                >= 2000 => VipLevel.Diamond, // 2000+ 钻石VIP
                >= 1000 => VipLevel.Royal,   // 1000+ 至尊VIP
                >= 500 => VipLevel.Ultimate, // 500+ 终极VIP
                >= 200 => VipLevel.Advanced, // 200+ 高级VIP
                >= 100 => VipLevel.Middle,   // 100+ 中级VIP
                >= 50 => VipLevel.Basic,     // 50+ 基础VIP
                >= 10 => VipLevel.None,      // 10+ 普通VIP
                _ => VipLevel.None
            };
        }

        public bool IsVip() => VipLevel > VipLevel.None && DateTime.Now < VipExpireTime;

        public int GetExpBonus() => IsVip() ? VipLevel switch
        {
            VipLevel.Basic => 5,
            VipLevel.Middle => 10,
            VipLevel.Advanced => 15,
            VipLevel.Ultimate => 20,
            VipLevel.Royal => 30,
            VipLevel.Diamond => 50,
            VipLevel.Gold => 80,
            _ => 0
        } : 0;

        public double GetDropBonus() => IsVip() ? VipLevel switch
        {
            VipLevel.Basic => 0.05,
            VipLevel.Middle => 0.10,
            VipLevel.Advanced => 0.15,
            VipLevel.Ultimate => 0.20,
            VipLevel.Royal => 0.30,
            VipLevel.Diamond => 0.50,
            VipLevel.Gold => 1.00,
            _ => 0.0
        } : 0.0;

        public int GetDailyGiftGold() => IsVip() ? VipLevel switch
        {
            VipLevel.Basic => 50000,
            VipLevel.Middle => 200000,
            VipLevel.Advanced => 500000,
            VipLevel.Ultimate => 1000000,
            VipLevel.Royal => 3000000,
            VipLevel.Diamond => 8000000,
            VipLevel.Gold => 20000000,
            _ => 0
        } : 0;

        public int GetMaxStallSlots() => IsVip() ? VipLevel switch
        {
            VipLevel.Basic => 5,
            VipLevel.Middle => 8,
            VipLevel.Advanced => 12,
            VipLevel.Ultimate => 16,
            VipLevel.Royal => 20,
            VipLevel.Diamond => 30,
            VipLevel.Gold => 50,
            _ => 2
        } : 2;

        public int GetWarehousePages() => IsVip() ? VipLevel switch
        {
            VipLevel.Basic => 2,
            VipLevel.Middle => 3,
            VipLevel.Advanced => 4,
            VipLevel.Ultimate => 5,
            VipLevel.Royal => 6,
            VipLevel.Diamond => 8,
            VipLevel.Gold => 10,
            _ => 1
        } : 1;

        #endregion

        #region 每日礼包

        /// <summary>
        /// 尝试领取每日礼包
        /// </summary>
        public void TryClaimDailyGift()
        {
            if (!IsVip())
            {
                _owner.SaySystem("您还不是VIP会员，无法领取每日礼包");
                return;
            }
            DateTime today = DateTime.Today;
            if (DateLastGift.Date == today)
            {
                _owner.SaySystem("今日礼包已领取，请明天再来");
                return;
            }
            DateLastGift = today;
            int gold = GetDailyGiftGold();
            if (gold > 0) _owner.AddGold((uint)gold);
            // VIP专属道具礼包（按等级）
            var items = GetDailyGiftItems();
            foreach (var (itemName, count) in items)
            {
                var itemDef = ItemManager.Instance.GetDefinitionByName(itemName);
                if (itemDef != null)
                {
                    var item = new ItemInstance(itemDef, (long)ItemManager.Instance.AllocateTempMakeIndex());
                    item.Count = count;
                    _owner.Inventory.AddItem(item);
                }
            }
            Save();
            _owner.SaySystem($"恭喜领取VIP{(int)VipLevel}每日礼包：{gold:N0}金币 + 道具");
        }

        private List<(string ItemId, int Count)> GetDailyGiftItems()
        {
            return VipLevel switch
            {
                VipLevel.Basic => new() { ("GA1", 1) },        // 初级祝福油 x1
                VipLevel.Middle => new() { ("GA1", 3), ("SW1", 1) },
                VipLevel.Advanced => new() { ("GA1", 5), ("SW1", 2), ("DAM", 1) },
                VipLevel.Ultimate => new() { ("GA1", 10), ("SW1", 5), ("DAM", 2), ("SC", 1) },
                VipLevel.Royal => new() { ("GA1", 20), ("SW1", 10), ("DAM", 5), ("SC", 3), ("GM3", 1) },
                VipLevel.Diamond => new() { ("GA1", 50), ("SW1", 30), ("DAM", 10), ("SC", 5), ("GM3", 3), ("GM4", 1) },
                VipLevel.Gold => new() { ("GA1", 100), ("SW1", 50), ("DAM", 20), ("SC", 10), ("GM3", 5), ("GM4", 3), ("GM5", 1) },
                _ => new()
            };
        }

        #endregion

        #region 经验加成（战斗中使用）

        /// <summary>
        /// 获得经验时调用此方法，返回实际应得经验（含VIP加成）
        /// </summary>
        public int ApplyExpBonus(int baseExp)
        {
            int bonus = GetExpBonus();
            return bonus > 0 ? baseExp * (100 + bonus) / 100 : baseExp;
        }

        #endregion

        #region VIP专属地图（安全区/练功区）

        /// <summary>
        /// VIP专属地图ID列表，-1表示无专属地图
        /// </summary>
        public int GetVipMapId()
        {
            return VipLevel switch
            {
                VipLevel.Gold => 2015,      // 钻石会员专属地图
                VipLevel.Diamond => 2015,
                VipLevel.Royal => 2014,     // 至尊会员专属地图
                VipLevel.Ultimate => 2013,  // 终极会员专属地图
                _ => -1
            };
        }

        public bool CanEnterVipMap(int mapId)
        {
            return IsVip() && mapId == GetVipMapId();
        }

        #endregion
    }

    public enum VipLevel
    {
        None = 0,
        None_10 = 1,   // 体验VIP(10元)
        Basic = 2,     // 基础VIP(50元)
        Middle = 3,    // 中级VIP(100元)
        Advanced = 4,  // 高级VIP(200元)
        Ultimate = 5, // 终极VIP(500元)
        Royal = 6,    // 至尊VIP(1000元)
        Diamond = 7,  // 钻石VIP(2000元)
        Gold = 8      // 钻石VIP(5000元)
    }

    internal class VipData
    {
        public int VipLevel { get; set; }
        public string VipExpireTime { get; set; } = "";
        public string DateLastGift { get; set; } = "";
        public int TotalRecharge { get; set; }
    }

    #endregion
    #endregion

}
