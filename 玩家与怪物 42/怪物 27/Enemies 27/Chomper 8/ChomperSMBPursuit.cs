using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Gamekit3D
{
    public class ChomperSMBPursuit : SceneLinkedSMB<ChomperBehavior>//咀嚼者的追赶互动
    {                                                       //动画控制器        动画控制器状态信息            层数
        public override void OnSLStateNoTransitionUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)//animator的使用方法 https://blog.csdn.net/linxinfa/article/details/94392971?utm_medium=distribute.pc_relevant.none-task-blog-baidujs-1
        {
            base.OnSLStateNoTransitionUpdate(animator, stateInfo, layerIndex);//当动画机状态为未转换状态时，每帧调用一次           base：调用父类成员方法

            m_MonoBehaviour.FindTarget();//发现目标时对位置进行记录

            if (m_MonoBehaviour.controller.navmeshAgent.pathStatus == NavMeshPathStatus.PathPartial 
                || m_MonoBehaviour.controller.navmeshAgent.pathStatus == NavMeshPathStatus.PathInvalid)//如果目标的路径状态等于该怪物的路径状态 或者 目标的当前路径状态是无效的，就停止追赶
            {
                m_MonoBehaviour.StopPursuit();//停止追赶
                return;
            }

            if (m_MonoBehaviour.target == null || m_MonoBehaviour.target.respawning)//如果失去目标或者目标处于重生状态，怪物停止追赶
            {//if the target was lost or is respawning, we stop the pursit
                m_MonoBehaviour.StopPursuit();//停止追赶
            }
            else//否则，继续追赶
            {
                m_MonoBehaviour.RequestTargetPosition();//获取目标位置

                Vector3 toTarget = m_MonoBehaviour.target.transform.position - m_MonoBehaviour.transform.position;//与目标的相差距离

                if (toTarget.sqrMagnitude < m_MonoBehaviour.attackDistance * m_MonoBehaviour.attackDistance)//如果相差距离的平方小于怪物攻击距离的平方，怪物就开始对目标攻击
                {
                    m_MonoBehaviour.TriggerAttack();
                }
                else if (m_MonoBehaviour.followerData.assignedSlot != -1)//该值默认为-1，如果分配批次为默认状态，就根据攻击距离和目标人物位置重新设置目标点
                {
                    Vector3 targetPoint = m_MonoBehaviour.target.transform.position + 
                        m_MonoBehaviour.followerData.distributor.GetDirection(m_MonoBehaviour.followerData
                            .assignedSlot) * m_MonoBehaviour.attackDistance * 0.9f;

                    m_MonoBehaviour.controller.SetTarget(targetPoint);
                }
                else//否则停止追赶
                {
                    m_MonoBehaviour.StopPursuit();
                }
            }
        }
    }
}