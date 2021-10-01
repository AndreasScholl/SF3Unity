using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shiningforce
{
    public class BattleScene : MonoBehaviour
    {
        BattleCamera _battleCamera = null;

        private float _battleCameraTime = 0f;

        GameObject _character1;
        GameObject _character2;

        Animation _anim1;
        Animation _anim2;

        private float _anim1Time = 0f;
        private float _anim2Time = 0f;

        private float _anim1Start = 0f;
        private float _anim2Start = 0f;

        public float _anim1StartTime = 1.8f;
        public float _anim2StartTime = 1.05f;

        public float _hitEffectDelay = 1.95f;
        private float _hitEffectTime = 0.0f;

        public float _endZPos = -13f;
        public float _endDampTime = 1f;
        public float _endMoveStart = 0.5f;

        public class EffectInfo
        {
            public string Prefab = "";
            public GameObject Parent;
            public string Node;
            public string Sound = "";
            public float Delay;
            public float Time = 0f;
            public bool OnGround = false;
        }

        public float _dust1Start = 1.4f;
        public float _dust2Start = 2.0f;
        public float _swordEffectStart = 1.475f;

        public List<EffectInfo> _effects = new List<EffectInfo>();

        private float _startDelay;

        public void Init(int character1, int character2)
        {
            _character1 = Loader.Instance.LoadObject(character1);
            _character2 = Loader.Instance.LoadObject(character2);

            _anim1 = _character1.GetComponent<Animation>();
            _anim2 = _character2.GetComponent<Animation>();

            const float characterBattleOffset = 9.25f;
            _character1.transform.position = new Vector3(0f, 0f, -25.6f + characterBattleOffset);
            _character1.transform.localEulerAngles = new Vector3(0f, 180f, 0f);

            _character2.transform.position = new Vector3(0f, 0f, -characterBattleOffset);

            // camera behind enemy
            Transform camTransform = Camera.main.transform;
            camTransform.position = new Vector3(3f, 2f, -29f);
            camTransform.eulerAngles = new Vector3(0f, -15f, 0f);

            _battleCamera = Camera.main.gameObject.AddComponent<BattleCamera>();

            EffectInfo effect = new EffectInfo();
            effect.Prefab = "DustImpact";
            effect.Parent = _character2;
            effect.Node = "node0";
            effect.OnGround = true;
            effect.Delay = _dust1Start;
            _effects.Add(effect);

            effect = new EffectInfo();
            effect.Prefab = "DustImpact";
            effect.Parent = _character2;
            effect.Node = "node0";
            effect.OnGround = true;
            effect.Delay = _dust2Start;
            _effects.Add(effect);

            effect = new EffectInfo();
            effect.Prefab = "SparksEffect";
            effect.Sound = "hit";
            effect.Parent = _character1;
            effect.Node = "subnode9";
            effect.OnGround = false;
            effect.Delay = _hitEffectDelay;
            _effects.Add(effect);

            effect = new EffectInfo();
            effect.Sound = "hit";
            effect.Delay = _hitEffectDelay - 0.2f;
            _effects.Add(effect);

            effect = new EffectInfo();
            effect.Sound = "sword";
            effect.Delay = _swordEffectStart;
            _effects.Add(effect);

            ResetBattle();

            _startDelay = 0.25f;
        }

        private void ResetBattle()
        {
            _battleCamera.CameraDistance = 15f;
            _battleCamera.CameraHeight = 2.5f;
            _battleCamera.LookAtHeight = 2.5f;
            _battleCamera.ViewAngle = 180f;
            _battleCamera.SetTargetPosition(new Vector3(0f, 0f, -12.8f));
            _battleCamera.SnapToTarget();

            _battleCameraTime = 0f;

            _anim1Start = 0f;
            _anim2Start = 0f;

            _hitEffectTime = 0f;

            for (int count = 0; count < _effects.Count; count++)
            {
                _effects[count].Time = 0f;
            }
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;

            _effects[0].Delay = _dust1Start;
            _effects[1].Delay = _dust2Start;
            _effects[4].Delay = _swordEffectStart;

            if (keyboard.f2Key.wasPressedThisFrame)
            {
                ResetBattle();
            }

            if (keyboard.f3Key.wasPressedThisFrame)
            {
                StartBattle();
            }

            if (_startDelay > 0f)
            {
                _startDelay -= Time.deltaTime;

                if (_startDelay <= 0f)
                {
                    _startDelay = 0f;
                    StartBattle();
                }
            }

            if (_battleCameraTime > 0f)
            {
                _battleCameraTime -= Time.deltaTime;

                if (_battleCameraTime <= 0f)
                {
                    _battleCameraTime = 0f;

                    _battleCamera.SetTargetPosition(new Vector3(0f, 0f, _endZPos));
                    _battleCamera.DampTime = _endDampTime;
                }
            }

            if (_anim1Start > 0f)
            {
                _anim1Start -= Time.deltaTime;

                if (_anim1Start <= 0f)
                {
                    _anim1Start = 0f;
                    PlayCharacter1Anim(1);
                }
            }

            for (int count = 0; count < _effects.Count; count++)
            {
                if (_effects[count].Time > 0f)
                {
                    _effects[count].Time -= Time.deltaTime;

                    if (_effects[count].Time <= 0f)
                    {
                        _effects[count].Time = 0f;

                        if (_effects[count].Prefab != "")
                        {
                            Vector3 effectPos = _effects[count].Parent.transform.position;
                            if (_effects[count].Node != "")
                            {
                                effectPos = TransformHelper.FindChild(_effects[count].Parent, _effects[count].Node).transform.position;
                            }
                            if (_effects[count].OnGround == true)
                            {
                                effectPos.y = 0.1f;
                            }
                            EffectManager.Instance.CreateOneShot(_effects[count].Prefab, effectPos);
                        }
                        if (_effects[count].Sound != "")
                        {
                            AudioPlayer.GetInstance().PlaySound(_effects[count].Sound);
                        }
                    }
                }
            }

            if (_anim2Start > 0f)
            {
                _anim2Start -= Time.deltaTime;

                if (_anim2Start <= 0f)
                {
                    _anim2Start = 0f;
                    PlayCharacter2Anim(3);
                }
            }

            if (_anim1Time > 0f)
            {
                _anim1Time -= Time.deltaTime;

                if (_anim1Time <= 0f)
                {
                    _anim1Time = 0f;

                    _anim1.Stop();
                    _anim1.Play("anim0");
                }
            }

            if (_anim2Time > 0f)
            {
                _anim2Time -= Time.deltaTime;

                if (_anim2Time <= 0f)
                {
                    _anim2Time = 0f;

                    _anim2.Stop();
                    _anim2.Play("anim0");
                }
            }
        }

        private void PlayCharacter1Anim(int index)
        {
            string animName = "anim" + index;

            _anim1[animName].wrapMode = WrapMode.ClampForever;

            _anim1.Play(animName);

            _anim1Time = _anim1[animName].length;
        }

        private void PlayCharacter2Anim(int index)
        {
            string animName = "anim" + index;

            _anim2[animName].wrapMode = WrapMode.ClampForever;

            _anim2.Play(animName);

            _anim2Time = _anim2[animName].length;
        }

        private void StartBattle()
        {
            _battleCamera.CameraDistance = 15f;
            _battleCamera.CameraHeight = 2.5f;
            _battleCamera.LookAtHeight = 2.5f;
            _battleCamera.ViewAngle = 45f;
            _battleCamera.SetTargetPosition(new Vector3(0f, 0f, -10f));
            _battleCamera.DampTime = 0.75f;

            _battleCameraTime = _endMoveStart;

            _anim1Start = _anim1StartTime;
            _anim2Start = _anim2StartTime;

            _hitEffectTime = _hitEffectDelay;

            for (int count = 0; count < _effects.Count; count++)
            {
                _effects[count].Time = _effects[count].Delay;
            }
        }
    }
}
