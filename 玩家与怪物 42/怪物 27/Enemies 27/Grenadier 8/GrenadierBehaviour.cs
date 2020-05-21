using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Gamekit3D
{
    [RequireComponent(typeof(EnemyController))]//怪物控制器
    [RequireComponent(typeof(NavMeshAgent))]//导航系统
    public class GrenadierBehaviour : MonoBehaviour//投掷者行为管理
    {
        public enum OrientationState//方向状态
        {
            IN_TRANSITION,//在转换位置
            ORIENTED_ABOVE,//玩家在上面
            ORIENTED_FACE//玩家在正面
        }

        public static readonly int hashInPursuitParam = Animator.StringToHash("InPursuit");//从字符串生成一个参数ID。 追赶参数 //详细内容请看： https://blog.csdn.net/googtiki/article/details/78636997?locationNum=2%20fps=1
        public static readonly int hashSpeedParam = Animator.StringToHash("Speed");//速度参数
        public static readonly int hashTurnAngleParam = Animator.StringToHash("Angle");//角度参数
        public static readonly int hashTurnTriggerParam = Animator.StringToHash("TurnTrigger");//触发器参数
        public static readonly int hashMeleeAttack = Animator.StringToHash("MeleeAttack");//近战攻击参数
        public static readonly int hashRangeAttack = Animator.StringToHash("RangeAttack");//远程攻击参数
        public static readonly int hashHitParam = Animator.StringToHash("Hit");//击打参数
        public static readonly int hashDeathParam = Animator.StringToHash("Death");//死亡参数
        public static readonly int hashRotateAttackParam = Animator.StringToHash("Rotate");//旋转参数

        public static readonly int hashIdleState = Animator.StringToHash("GrenadierIdle");//投掷者闲置参数

        public EnemyController controller { get { return m_EnemyController; } }//获取怪物控制器

        public TargetScanner playerScanner;//用于寻找玩家位置

        public float meleeRange = 4.0f;//近战范围
        public float rangeRange = 10.0f;//远程范围

        public MeleeWeapon fistWeapon;//拳头
        public RangeWeapon grenadeLauncher;//榴弹发射器

        public GameObject shield;//盾

        public SkinnedMeshRenderer coreRenderer;//蒙皮网格过滤器：皮并不是模型的贴图，而是 Mesh 本身，蒙皮是指将 Mesh 中的顶点附着（绑定）在骨骼之上。骨骼控制蒙皮运动，动画控制骨骼的运动。
                                                //关于该网格过滤器与其他的mesh的区别请看：https://blog.csdn.net/linxinfa/article/details/88695474
        protected EnemyController m_EnemyController;//怪物控制器
        protected NavMeshAgent m_NavMeshAgent;//寻路功能

        public bool shieldUp { get { return shield.activeSelf; } }//将盾显现出来

        public PlayerController target { get { return m_Target; } }//获取目标
        public Damageable damageable { get { return m_Damageable; } }//获取可破坏的物体

        [Header("Audio")]
        public RandomAudioPlayer deathAudioPlayer;//玩家死亡音效
        public RandomAudioPlayer damageAudioPlayer;//造成伤害音效
        public RandomAudioPlayer footstepAudioPlayer;//脚步声
        public RandomAudioPlayer throwAudioPlayer;//玩家掷物音效
        public RandomAudioPlayer punchAudioPlayer;//玩家出拳的音效

        protected PlayerController m_Target;
        //用于存储掷弹兵决定射击时目标的位置 used to store the position of the target when the Grenadier decide to shoot, so if the player
        //在开始动画和手榴弹发射时进行移动，如果它不在现在的位置，它就会射击  move between the start of the animation and the actual grenade launch, it shoot were it was not where it is now
        protected Vector3 m_GrenadeTarget;//发射的目标点
        protected Material m_CoreMaterial;//核心材质

        protected Damageable m_Damageable;//可破坏的物体
        protected Color m_OriginalCoreMaterial;//原始核心材质

        protected float m_ShieldActivationTime;//盾的启动时间


        void OnEnable()//游戏开始前的准备
        {
            m_EnemyController = GetComponent<EnemyController>();//获取怪物的敌人控制器组件
            m_NavMeshAgent = GetComponent<NavMeshAgent>();//获得怪物的寻路组件

            SceneLinkedSMB<GrenadierBehaviour>.Initialise(m_EnemyController.animator, this);

            fistWeapon.SetOwner(gameObject);//设置可以使用拳击的对象
            fistWeapon.EndAttack();//默认拳击为停止状态

            m_CoreMaterial = coreRenderer.materials[1];//将核心材质替换为蒙皮过滤器的2号材质
            m_OriginalCoreMaterial = m_CoreMaterial.GetColor("_Color2");//将基础颜色设为核心材质的_Color2的颜色
                                             
            m_EnemyController.animator.Play(hashIdleState, 0, Random.value);//播放一个随机进度的投掷者站立动画   https://docs.unity3d.com/ScriptReference/Animator.Play.html

            shield.SetActive(false);//盾关闭

            m_Damageable = GetComponentInChildren<Damageable>();//获得子物体的伤害控制组件
        }

        private void Update()
        {

            if (m_ShieldActivationTime > 0)//如果盾的持续时间大于0，则随现实时间将它的持续时间减少
            {
                m_ShieldActivationTime -= Time.deltaTime;

                if (m_ShieldActivationTime <= 0.0f)//如果盾持续时间小于等于0，盾失效
                    DeactivateShield();
            }
        }

        public void FindTarget()//发现目标时存储目标的位置
        {
            m_Target = playerScanner.Detect(transform);
        }

        public void StartPursuit()//开始追赶
        {
            m_EnemyController.animator.SetBool(hashInPursuitParam, true);//追赶动画打开
        }

        public void StopPursuit()//停止追赶
        {
            m_EnemyController.animator.SetBool(hashInPursuitParam, false);//追赶动画关闭
    }

        public void StartAttack()//开始攻击
        {
            fistWeapon.BeginAttack(true);//将近战攻击设置为开启状态
        }

        public void EndAttack()//停止攻击
        {
            fistWeapon.EndAttack();//将近战设置为停止
        }

        public void Hit()//击打
        {
            damageAudioPlayer.PlayRandomClip();//将伤害音效打开
            m_EnemyController.animator.SetTrigger(hashHitParam);//将击打动画打开
            m_CoreMaterial.SetColor("_Color2", Color.red);//把核心材质颜色设置为红色
        }

        public void Die()//死亡
        {
            deathAudioPlayer.PlayRandomClip();//播放死亡时的音效
            m_EnemyController.animator.SetTrigger(hashDeathParam);//开启死亡动画
        }

        public void ActivateShield()//激活盾
        {
            shield.SetActive(true);//盾开启
            m_ShieldActivationTime = 3.0f;//盾的持续时间
            m_Damageable.SetColliderState(false);//无法对此物体继续造成伤害
        }

        public void DeactivateShield()//盾失效
        {
            shield.SetActive(false);//盾关闭
            m_Damageable.SetColliderState(true);//其他物体可以对其造成伤害
        }

        public void ReturnVulnerable()//
        {
            m_CoreMaterial.SetColor("_Color2", m_OriginalCoreMaterial);//将核心材质的颜色设为基本颜色
        }

        public void RememberTargetPosition()//记录投掷位置
        {
            m_GrenadeTarget = m_Target.transform.position;
        }

        public void PlayStep()//开始走动
        {
            footstepAudioPlayer.PlayRandomClip();//脚步声开启
        }

        public void Shoot()//远程攻击
        {
            throwAudioPlayer.PlayRandomClip();//投掷音效开启

            Vector3 toTarget = m_GrenadeTarget - transform.position;//记录与玩家的相差距离

            //手雷在玩家的“前方”几米处发射，因为它会反弹和滚动，玩家将无法得知它的具体落点 the grenade is launched a couple of meters in "front" of the player, because it bounce and roll, to make it a bit ahrder for the player
            //to avoid it
            Vector3 target = transform.position + (toTarget - toTarget * 0.3f);//投掷目标点

            grenadeLauncher.Attack(target);//远程攻击目标点
        }

        public OrientationState OrientTowardTarget()//定向到目标
        {
            Vector3 v = m_Target.transform.position - transform.position;//玩家与怪物之间相差距离的向量
            bool above = v.y > 0.3f;//当玩家与怪物在y轴相差距离大于0.3，则认为玩家在上方
            v.y = 0;//设y轴相差距离为0

            float angle = Vector3.SignedAngle(transform.forward, v, Vector3.up);//以(0,1,0)为轴，计算从(0,0,1)到v的夹角，此夹角有正负之分

            if (Mathf.Abs(angle) < 20.0f)//角度的绝对值小于20
            { //对于非常小的角度，我们直接旋转模型 for a very small angle, we directly rotate the model
                transform.forward = v.normalized;//当前向量不改变，返回一个新的规范化的向量
                // 如果玩家在玩家之上，我们返回false来告诉玩家空闲状态 if the player was above the player we return false to tell the Idle state 
                // 我们想要一个“盾牌”攻击，因为我们的拳击无法攻击它 that we want a "shield up" attack as our punch attack wouldn't reach it.
                return above ? OrientationState.ORIENTED_ABOVE : OrientationState.ORIENTED_FACE; //判断玩家是在怪物上面还是面前
            }

            m_EnemyController.animator.SetFloat(hashTurnAngleParam, angle / 180.0f);//开启转角动画，第二个参数影响动画的过渡时间
            m_EnemyController.animator.SetTrigger(hashTurnTriggerParam);//开启触发转换的动画
            return OrientationState.IN_TRANSITION;//怪物在转换位置
        }

#if UNITY_EDITOR//如果处于编辑状态，执行如下代码

        private void OnDrawGizmosSelected()
        {
           playerScanner.EditorGizmo(transform);//对怪物所在地的进行显示
        }

#endif
    }
}