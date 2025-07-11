
using ImGuiNET;
using SharpPluginLoader.Core;
using SharpPluginLoader.Core.Entities;
using System.ComponentModel.Design;
using System.Numerics;

namespace unaiZinogre
{
    public class unaiZinogre : IPlugin
    {
        public string Name => "Unchained Zinogre";
        public string Author => "seka";

        private bool _inQuest = false;
        private bool _oneZinoDies = false;
        private bool _bellowSecondBool = false;
        private bool _bellowFirstBool = false;
        private bool _zapBool = false;
        private bool _chargeState = false;
        private Monster? _zinoOne = null;
        private uint _lastStage = 0;
        private bool _shortPunch = false;
        private int _previousAction;
        private int _frameCounter;

        private bool _modEnabled = false;
        private string _statusMessage = "";
        private int _frameCountdown = 0;
        private const int _framesForMessage = 60;

        private Vector3 _targetPositionA;
        private Vector3 _targetPositionB;
        private Vector3 _targetPositionC;

        private float _myTimer = 0f;
        private int _timerElapsed = 0;


        private void ResetState()
        {
            // does not include _inQuest
            _zinoOne = null;
            _oneZinoDies = false;
            _shortPunch = false;
            _frameCounter = 0;
            _myTimer = 0f;
            _timerElapsed = 0;
            Monster.EnableSpeedReset();
            _bellowSecondBool = false;
            _bellowFirstBool = false;
            _zapBool = false;
            _chargeState = false;
        }
        public void OnMonsterDeath(Monster monster)
        {

            if (_zinoOne is not null && monster.Type == MonsterType.Zinogre)
            {
                _zinoOne = null;
                // _oneZinoDies = false;
                _shortPunch = false;
                _frameCounter = 0;
                _myTimer = 0f;
                _timerElapsed = 0;
                // Monster.EnableSpeedReset
                _bellowSecondBool = false;
                _bellowFirstBool = false;
                _zapBool = false;
                _chargeState = false;
            }
        }
        public void OnMonsterDestroy(Monster monster)
        {
            if (_zinoOne is not null && monster.Type == MonsterType.Zinogre)
            // watch out for using ResetState here, because if Narga decayed all values are reset
            {
                _zinoOne = null;
                _oneZinoDies = false;
                _shortPunch = false;
                _frameCounter = 0;
                _myTimer = 0f;
                _timerElapsed = 0;
                // Monster.EnableSpeedReset
                _bellowSecondBool = false;
                _bellowFirstBool = false;
                _zapBool = false;
                _chargeState = false;
            }
        }
        public void OnQuestEnter(int questId) // different from Quest Accept
        {
            _inQuest = true;
            // do not ResetState because OnMonsterCreate occurs before OnQuestEnter
            // _zinoOne = null;
            _oneZinoDies = false;
            _shortPunch = false;
            _frameCounter = 0;
            _myTimer = 0f;
            _timerElapsed = 0;
            Monster.EnableSpeedReset();
            _bellowSecondBool = false;
            _bellowFirstBool = false;
            _zapBool = false;
            _chargeState = false;
        }

        public void OnMonsterCreate(Monster monster)
        {
            _lastStage = (uint)Area.CurrentStage;

            if (monster.Type == MonsterType.Zinogre)
            {
                _zinoOne = monster;
                _oneZinoDies = false;
            }
        }

        public void OnQuestLeave(int questId) { ResetState(); _inQuest = false; }
        public void OnQuestComplete(int questId) { ResetState(); _inQuest = false; }
        public void OnQuestFail(int questId) { ResetState(); _inQuest = false; }
        public void OnQuestReturn(int questId) { ResetState(); _inQuest = false; }
        public void OnQuestAbandon(int questId) { ResetState(); _inQuest = false; }
        public void OnQuestAccept(int questId)
        {
            ResetState();
            _inQuest = false;
            // cannot take questId yet
        }
        public void OnQuestCancel(int questId) { ResetState(); _inQuest = false; }

        public void OnMonsterAction(Monster monster, ref int actionId) // the actionId on initiation was already called
        {
            var player = Player.MainPlayer;
            if (player is null) return;
            if (!_modEnabled) return;
            if (!_inQuest) return;

            if (_zinoOne is null && monster.Type == MonsterType.Zinogre)
            {
                _zinoOne = monster; // in case for multiple Zinogre, the next one alive will carry on the function
            }

            if (monster.Type != MonsterType.Zinogre) return;

            if (_zinoOne is not null && _zinoOne.Type == MonsterType.Zinogre)
            {
                if (actionId == Actions.PUNCH_L_LIGHTNING || actionId == Actions.PUNCH_R_LIGHTNING)
                {
                    Monster.DisableSpeedReset();
                    _shortPunch = true;
                }
                else if (_shortPunch)
                {
                    switch (_previousAction)
                    {
                        case Actions.PUNCH_L_LIGHTNING:
                            actionId = Actions.STRONG_PUNCH_COMBO_CONTINUE_R_LIGHTNING;
                            break;
                        case Actions.STRONG_PUNCH_COMBO_CONTINUE_R_LIGHTNING:
                            _shortPunch = false;
                            break;
                        case Actions.PUNCH_R_LIGHTNING:
                            actionId = Actions.STRONG_PUNCH_COMBO_CONTINUE_L_LIGHTNING;
                            break;
                        case Actions.STRONG_PUNCH_COMBO_CONTINUE_L_LIGHTNING:
                            _shortPunch = false;
                            break;
                    }
                }

                if (actionId == Actions.CHARGE_TRANS || actionId == Actions.CHARGE_TRANS2 || actionId == Actions.LIGHTNING_CHARGE_MAX)
                {
                    _chargeState = true;
                }

                if (actionId == Actions.DAMAGE_DROP_TO_GROUND_LIGHTNING_RELEASE || actionId == Actions.DAMAGE_HIGH_LIGHTNING_RELEASE)
                {
                    _chargeState = false;
                }

                if (_previousAction >= 113) // all damage
                {
                    _shortPunch = false;
                }


                _frameCounter = 0;
                _myTimer = 0f;
                _timerElapsed = 0;
                _zapBool = false;
                _bellowSecondBool = false;
                _bellowFirstBool = false;
                _previousAction = actionId;
                Monster.EnableSpeedReset();
            }
        }

        public unsafe void OnImGuiRender()
        {
            var player = Player.MainPlayer;
            if (player == null) return;

            if (ImGui.Button("Toggle"))
            {
                if (Quest.CurrentQuestId == -1)
                {
                    _modEnabled = !_modEnabled;
                    _statusMessage = _modEnabled ? "Plugin Enabled." :
                        "Plugin Disabled. To fully disable the mod, " +
                        "delete or rename the 'data' folder in: " +
                        "nativepc\\em\\em057\\00\\data\\";
                    ResetState();

                }
                else
                {
                    _statusMessage = "Cannot toggle while in quest.";
                }
                _frameCountdown = _framesForMessage;
            }
            if (_frameCountdown > 0)
            {
                ImGui.Text(_statusMessage);
            }

        }

        public void OnUpdate(float deltaTime)
        {
            var player = Player.MainPlayer;
            if (player is null) return;

            if ((uint)Area.CurrentStage != _lastStage)
            {
                ResetState();
            }

            if (!_modEnabled) return;
            if (!_inQuest) return;

            if (_zinoOne is null) return;


            var currentActionId = _zinoOne?.ActionController.CurrentAction.ActionId;
            if (_zinoOne is null) return;

            Vector3 upVector = new Vector3(0f, 1f, 0f); //normalized manually as 0,1,0
            // var normalizedSide = Vector3.Normalize(Vector3.Cross(_zinoOne.Forward, upVector));  // normalize the cross product
            // if (normalizedSide == Vector3.Zero) return;

            if (_zinoOne.Type == MonsterType.Zinogre && (currentActionId == Actions.STRONG_PUNCH_COMBO_CONTINUE_L_LIGHTNING || currentActionId == Actions.STRONG_PUNCH_COMBO_CONTINUE_R_LIGHTNING))
            {
                if (!_shortPunch)
                {
                    // Log.Info($"StrongPunch executed, previousAction:{_previousAction}, _shortPunch == {_shortPunch}");
                    // IMPORTANT. Without this, the follow up after Counter will be too fast.
                    return;
                }

                // Log.Info($"StrongPunch executed, previousAction:{_previousAction}, _shortPunch == {_shortPunch}");

                if (_zinoOne.AnimationFrame / _zinoOne.MaxAnimationFrame <= 0.6)
                {
                    Monster.DisableSpeedReset(); _zinoOne.Speed = 2.5f;
                }
                else if (_zinoOne.AnimationFrame / _zinoOne.MaxAnimationFrame <= 0.605)
                {
                    Monster.EnableSpeedReset();
                }
                else
                {
                    Monster.EnableSpeedReset();
                }

                _myTimer += deltaTime;
                if (_myTimer >= 0.1f)
                {
                    _myTimer -= 0.1f;
                    _timerElapsed++;

                    if (!_zapBool && _timerElapsed % 5 == 0)
                    {
                        _targetPositionC = _zinoOne.Position;
                    }
                    else if (!_zapBool && _timerElapsed % 6 == 0)
                    {
                        _targetPositionC.Y += 15f;
                        _zinoOne.CreateShell(0, 19, _zinoOne.Position, _targetPositionC);
                        _zapBool = true;
                    }
                }
            }

            if (_zinoOne.Type == MonsterType.Zinogre && (currentActionId == Actions.PUNCH_L_LIGHTNING || currentActionId == Actions.PUNCH_R_LIGHTNING))
            {
                _myTimer += deltaTime;

                if (_myTimer >= 0.1f) //this is in seconds
                {
                    _myTimer -= 0.1f; //have to reset it otherwise it will always fulfill >= 0.1f and thus _timerElapsed++ every frame
                    _timerElapsed++; // 17 18 19 20 21 22 ... 34,36,38,40,42,44 ... 48

                    if (!_zapBool && _timerElapsed % 12 == 0) // remember this applies to _timerElapsed == 24 and 36 as well
                    {
                        Monster.DisableSpeedReset(); _zinoOne.Speed = 0.33f;
                        _targetPositionA = _zinoOne.Position;
                        _targetPositionB = _zinoOne.Position;
                    }
                    else if (!_zapBool && (_timerElapsed % 17 == 0 || _timerElapsed % 19 == 0 || _timerElapsed % 21 == 0))
                    {
                        _targetPositionA.X += 5f; _targetPositionA.Y += 15f;
                        _zinoOne.CreateShell(0, 6, _zinoOne.Position, _targetPositionA);
                        _zinoOne.CreateShell(0, 17, _zinoOne.Position, _targetPositionA);
                    }
                    else if (!_zapBool && (_timerElapsed % 18 == 0 || _timerElapsed % 20 == 0 || _timerElapsed % 22 == 0))
                    {
                        _targetPositionB.Z += 5f; _targetPositionB.Y += 15f;
                        _zinoOne.CreateShell(0, 6, _zinoOne.Position, _targetPositionB);
                        _zinoOne.CreateShell(0, 17, _zinoOne.Position, _targetPositionB);
                    }
                    else if (!_zapBool && _timerElapsed % 48 == 0)
                    {
                        Monster.DisableSpeedReset(); _zinoOne.Speed = 2f;
                        _zinoOne.AnimationFrame = _zinoOne.MaxAnimationFrame;
                        _zapBool = true;
                    }
                }
            }

            if (_zinoOne.Type == MonsterType.Zinogre && currentActionId == Actions.BELOW_ATTACK)
            {
                /*if (_zinoOne.AnimationFrame / _zinoOne.MaxAnimationFrame < 0.500)
                {
                    _targetPositionA = _zinoOne.Position;
                    _targetPositionB = _zinoOne.Position;
                    _targetPositionC = _zinoOne.Position;
                    // Log.Info($"first bracket");
                }
                else if (!_bellowSecondBool && !_bellowFirstBool && _zinoOne.AnimationFrame / _zinoOne.MaxAnimationFrame < 0.505)
                {
                    _bellowFirstBool = true;
                    _targetPositionA += _zinoOne.Forward * 20f;
                    _targetPositionB.X += 10f;
                    _targetPositionC.X += 10f;
                    // Log.Info($"second bracket");
                } 
                else if (!_bellowSecondBool && _bellowFirstBool && _zinoOne.AnimationFrame / _zinoOne.MaxAnimationFrame < 0.505)
                {
                    _bellowFirstBool = false;
                    _bellowSecondBool = true; 
                    // Log.Info($"third bracket");
                }
                
                if (_bellowSecondBool) 
                {
                    if (_chargeState)
                    {
                        _zinoOne.CreateShell(0, 19, _zinoOne.Position, _targetPositionA);
                    }
                    else
                    {
                        _zinoOne.CreateShell(0, 6, _zinoOne.Position, _targetPositionA);
                        _zinoOne.CreateShell(0, 6, _zinoOne.Position, _targetPositionB);
                        _zinoOne.CreateShell(0, 6, _zinoOne.Position, _targetPositionC);
                    }
                    _bellowFirstBool = false;
                    _bellowSecondBool = false;
                } */

                _myTimer += deltaTime;
                var normalizedSide = Vector3.Normalize(Vector3.Cross(_zinoOne.Forward, upVector));
                if (normalizedSide == Vector3.Zero) return;

                if (_myTimer >= 0.1f)
                {
                    _myTimer -= 0.1f;
                    _timerElapsed++;

                    if (_timerElapsed == 5)
                    {
                        _targetPositionA = _zinoOne.Position;
                        _targetPositionB = _zinoOne.Position;
                        _targetPositionC = _zinoOne.Position;
                    }
                    else if (_timerElapsed == 19)
                    {
                        _targetPositionA += _zinoOne.Forward * 500f;
                        _targetPositionB += _zinoOne.Forward * 500f;
                        _targetPositionC += _zinoOne.Forward * 500f;

                        _targetPositionB += normalizedSide * 200f;
                        _targetPositionC -= normalizedSide * 200f;

                        if (_chargeState)
                        {
                            _zinoOne.CreateShell(0, 19, _zinoOne.Position, _targetPositionA);
                        }
                        else
                        {
                            _zinoOne.CreateShell(0, 6, _zinoOne.Position, _targetPositionA);
                            _zinoOne.CreateShell(0, 6, _zinoOne.Position, _targetPositionB);
                            _zinoOne.CreateShell(0, 6, _zinoOne.Position, _targetPositionC);
                        }
                    }
                }
            }

            if (_zinoOne.Type == MonsterType.Zinogre && currentActionId == Actions.SIDE_ATTACK)
            {
                /*if (_zinoOne.AnimationFrame / _zinoOne.MaxAnimationFrame < 0.180)  { }
                else if (!_zapBool && _zinoOne.AnimationFrame / _zinoOne.MaxAnimationFrame < 0.185)
                {
                    _zapBool = true;
                }

                if (_zapBool)
                {
                    _myTimer += deltaTime;

                    if (_myTimer >= 0.1f)
                    {
                        _myTimer -= 0.1f;
                        _timerElapsed++;

                        if (_timerElapsed == 2)
                        {
                            _zinoOne.CreateShell(0, 17, _zinoOne.Position, player.Position);
                        }
                        else if (_timerElapsed == 6)
                        {
                            Log.Info($"_timerElapsed == 6 check true {_timerElapsed}");
                            if (_chargeState)
                            {
                                Log.Info($"Shell 21");
                                _zinoOne.CreateShell(0, 21, player.Position, _zinoOne.Position);
                            }
                            else
                            {
                                Log.Info($"Shell 13");
                                _zinoOne.CreateShell(0, 13, player.Position, _zinoOne.Position);
                            }
                        }
                    }
                }*/

                _myTimer += deltaTime;
                var normalizedSide = Vector3.Normalize(Vector3.Cross(player.Forward, upVector));
                if (normalizedSide == Vector3.Zero) return;

                if (_myTimer >= 0.1f)
                {
                    _myTimer -= 0.1f;
                    _timerElapsed++;

                    if (_timerElapsed == 1)
                    {
                        _targetPositionA = player.Position;
                        _targetPositionB = player.Position;
                        _targetPositionC = player.Position;
                    }
                    else if (_timerElapsed == 7)
                    {
                        _targetPositionC -= _zinoOne.Position; 
                        _targetPositionC /= 2; 
                        _targetPositionA -= _targetPositionC; 

                        if (_chargeState)
                        {
                            _zinoOne.CreateShell(0, 17, _zinoOne.Position, player.Position);
                            _zinoOne.CreateShell(0, 17, _zinoOne.Position, _targetPositionA);

                            //Log.Info($"A {_targetPositionA}");
                            //Log.Info($"player {player.Position}");
                            //Log.Info($"monster {_zinoOne.Position}");
                        }
                    }
                    else if (_timerElapsed == 20)
                    {
                        if (_chargeState)
                        {
                            _zinoOne.CreateShell(0, 21, _targetPositionB, _targetPositionA);
                        }
                        else
                        {
                            _zinoOne.CreateShell(0, 13, player.Position, _zinoOne.Position);
                        }
                    }
                    //Log.Info($"SideAttack every 0.1s _timerElapsed == {_timerElapsed}");
                }
            }

            if (_zinoOne.Type == MonsterType.Zinogre && currentActionId == Actions.SHOULDER_TACKLE)
            {
                _myTimer += deltaTime;

                if (_myTimer >= 0.1f)
                {
                    _myTimer -= 0.1f;
                    _timerElapsed++;
                    var normalizedSide = Vector3.Normalize(Vector3.Cross(player.Forward, upVector));
                    if (normalizedSide == Vector3.Zero) return;

                    if (_timerElapsed == 1)
                    {
                        _targetPositionA = player.Position;
                        _targetPositionB = player.Position;
                        _targetPositionC = player.Position;
                    }
                    else if (_timerElapsed == 4)
                    {
                        _targetPositionC -= _zinoOne.Position; // player(originalC) - zino = distance which C now takes upon itself as distance unit
                        _targetPositionC /= 2; //half distance
                        _targetPositionA -= _targetPositionC; // originalA - half distance(currentC) = midpoint between player(originalA) and zino

                        if (_chargeState)
                        {
                            _zinoOne.CreateShell(0, 17, _zinoOne.Position, player.Position);
                            _zinoOne.CreateShell(0, 17, _zinoOne.Position, _targetPositionA);

                            // Log.Info($"A {_targetPositionA}");
                            // Log.Info($"player {player.Position}");
                            // Log.Info($"monster {_zinoOne.Position}");
                        }
                    }
                    else if (_timerElapsed == 17)
                    {
                        if (_chargeState)
                        {
                            _zinoOne.CreateShell(0, 21, _targetPositionB, _targetPositionA);
                        }
                        else
                        {
                            _zinoOne.CreateShell(0, 13, player.Position, _zinoOne.Position);
                        }
                    }
                    // Log.Info($"Shoulder every 0.1s _timerElapsed == {_timerElapsed}");
                }
            }

            if (_zinoOne.Type == MonsterType.Zinogre && (currentActionId == Actions.FOX_WALK || currentActionId == Actions.FOX_WALK_FROM_LIGHTNING_BALL))
            {
                if (_zinoOne.AnimationFrame / _zinoOne.MaxAnimationFrame < 0.100) { }
                else if (!_zapBool && _zinoOne.AnimationFrame / _zinoOne.MaxAnimationFrame < 0.105)
                {
                    _zapBool = true;
                }
                else if (_zinoOne.AnimationFrame / _zinoOne.MaxAnimationFrame < 0.400) { }
                else if (!_zapBool && _zinoOne.AnimationFrame / _zinoOne.MaxAnimationFrame < 0.405)
                {
                    _zapBool = true;
                }
                else if (_zinoOne.AnimationFrame / _zinoOne.MaxAnimationFrame < 0.700) { }
                else if (!_zapBool && _zinoOne.AnimationFrame / _zinoOne.MaxAnimationFrame < 0.705)
                {
                    _zapBool = true;
                }

                if (_zapBool)
                {
                    _frameCounter++;

                    if (_frameCounter % 3 == 0)
                    {
                        _zinoOne.CreateShell(0, 17, _zinoOne.Position, player.Position);
                        _frameCounter = 0;
                        _zapBool = false;
                    }
                }
            }

            if (_zinoOne.Type == MonsterType.Zinogre && currentActionId == Actions.MULTI_COMBO_ATTACK_END)
            {
                if (_zinoOne.AnimationFrame / _zinoOne.MaxAnimationFrame <= 0.2)
                {
                    Monster.EnableSpeedReset();
                }
                else if (_zinoOne.AnimationFrame / _zinoOne.MaxAnimationFrame <= 0.99)
                {
                    Monster.DisableSpeedReset(); _zinoOne.Speed = 2.5f;
                }
                else
                {
                    Monster.EnableSpeedReset();
                }
            }

            if (_zinoOne.Type == MonsterType.Zinogre && (currentActionId == Actions.BACK_SHOWERED_ATTACK_MAIN_L || currentActionId == Actions.BACK_SHOWERED_ATTACK_MAIN_R))
            {
                _myTimer += deltaTime;

                if (_myTimer >= 0.1f)
                {
                    _myTimer -= 0.1f;
                    _timerElapsed++;

                    if (_timerElapsed % 30 == 0)
                    {
                        Monster.DisableSpeedReset(); _zinoOne.Speed = 1.7f;
                    }
                }
            }

            if (_zinoOne.Type == MonsterType.Zinogre && currentActionId == Actions.SOMERSAULT)
            {
                _myTimer += deltaTime;

                if (_myTimer >= 0.1f)
                {
                    _myTimer -= 0.1f;
                    _timerElapsed++;

                    if (_timerElapsed % 30 == 0)
                    {
                        Monster.DisableSpeedReset(); _zinoOne.Speed = 2f;
                    }
                }
            }

            if (_zinoOne.Type == MonsterType.Zinogre && currentActionId == Actions.LIGHTNING_CHARGE_MIN)
            {
                /*if (_zinoOne.AnimationFrame / _zinoOne.MaxAnimationFrame < 0.500) 
                {
                    _zinoOne.AnimationFrame = _zinoOne.MaxAnimationFrame * 0.500f;
                }*/

                _myTimer += deltaTime;

                if (_myTimer >= 0.1f)
                {
                    _myTimer -= 0.1f;
                    _timerElapsed++;

                    if (_timerElapsed == 8 || _timerElapsed == 24) // normally ~6frames each 0.1s but these check every 0.1s
                    {
                        _targetPositionA = player.Position;
                        _targetPositionB = player.Position;
                    }
                    else if (_timerElapsed == 13 || _timerElapsed == 29)
                    {
                        // Log.Info($"_timerElapsed == 28 check if true: {_timerElapsed}");
                        _zinoOne.CreateShell(0, 17, _zinoOne.Position, _targetPositionA); //pre-zap
                        _targetPositionB.Y += 30f;
                    }
                    else if (_timerElapsed == 22 || _timerElapsed == 39)
                    {
                        // Log.Info($"_timerElapsed == 30 or 52 check if true: {_timerElapsed}");
                        _zinoOne.CreateShell(0, 13, _targetPositionB, _targetPositionA); //bugs
                    }
                }
            }


            if (_zinoOne.Type == MonsterType.Zinogre && (currentActionId == Actions.CHARGE_TRANS || currentActionId == Actions.CHARGE_TRANS2))
            {
                Monster.DisableSpeedReset(); _zinoOne.Speed = 1.6f;

                if (_zinoOne.AnimationFrame / _zinoOne.MaxAnimationFrame < 0.400) { }
                else if (!_zapBool && _zinoOne.AnimationFrame / _zinoOne.MaxAnimationFrame < 0.405)
                {
                    _zapBool = true;
                }

                if (_zapBool)
                {
                    _frameCounter++;

                    if (_frameCounter % 3 == 0)
                    {
                        _zinoOne.CreateShell(0, 23, player.Position, _zinoOne.Position);
                        _zapBool = false;
                        _frameCounter = 0;
                    }

                }
            }

            if (_zinoOne.Type == MonsterType.Zinogre && (currentActionId == Actions.BACK_TURN || currentActionId == Actions.CATCH_ATTACK))
            {
                Monster.DisableSpeedReset(); _zinoOne.Speed = 1.2f;
            }

            if (_zinoOne.Type == MonsterType.Zinogre && (currentActionId == Actions.JUMP_ATTACK || currentActionId == Actions.JUMP_ATTACK_LIGHTNING))
            {
                Monster.DisableSpeedReset(); _zinoOne.Speed = 1.15f;
            }

            // CurrentAction is a property of type ActionInfo&, and it has a getter({ get; }) which means it can only be read, not set directly.

            // The & symbol here suggests that CurrentAction returns a reference to an ActionInfo object rather than a copy of it.

            // ActionId is a field of ActionInfo, holding the value for CurrentAction

            if (_oneZinoDies)
            {
                ResetState();
            }
        }
    }
}
