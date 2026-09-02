1. Node.cs
   길찾기에 필요한 노드를 구현한 클래스로, 다음과 같은 정보를 담고 있다.
   걸을 수 있는 노드인지
   실제 월드 좌표가 몇인지
   격자에서 몇 콤마 몇인지
   부모 노드
   f/g/h 비용
   heapindex

2. NodeHeap.cs
   Node를 담는 Heap을 관리하는 클래스.
   Add : 노드 추가
   RemoveFirst : 0번 인덱스의 노드 제거하여 return
   UpdateItem : 해당 노드를 맞는 위치에 정렬
   Contains : heap에 해당 노드가 있는지

3. PathGrid.cs
   월드를 XZ 평면 위의 격자 노드들로 쪼개고, 각 칸이 walkable한지 판정해서 들고 있는 클래스.
   BuildGrid : 격자 크기를 계산하고 Physics.CheckBox로 각 칸의 장애물 여부를 검사해 Node 배열을 채운다. Awake에서 자동 호출.
   NodeFromWorldPoint : 월드 좌표가 속한 Node를 반환. Pathfinder.FindPath와 IsWalkable에서 참조.
   GetNeighbors : 어떤 노드의 이동 가능한 이웃 노드들을 반환(대각선 코너컷 방지 포함). Pathfinder.FindPath에서 참조.
   IsWalkable : 월드 좌표가 walkable한지 반환. Pathfinder.IsWalkable, ChaserLocomotion.GetWanderTarget에서 참조.
   OnDrawGizmos : Scene 뷰에서 격자와 DebugPath를 색으로 시각화.

4. Pathfinder.cs
   PathGrid 위에서 그리드 A*를 돌려 실제 경로(월드 좌표 리스트)를 계산하는 클래스.
   IsWalkable : PathGrid.IsWalkable을 그대로 전달하는 래퍼. ChaserLocomotion.GetWanderTarget에서 참조.
   FindPath : 시작~목표 사이 A* 탐색 후 waypoint 리스트 반환. ChaserLocomotion.MoveTo, (구버전)ChaserAgent.RequestPath에서 참조.
   Heuristic : 대각선 거리 기반 휴리스틱 비용 계산. FindPath 내부에서만 사용.
   Distance : 인접 노드 사이 실제 이동 비용(직선 10 / 대각선 14). FindPath 내부에서만 사용.
   RetracePath : 목표 노드에서 Parent를 거슬러 경로를 역추적하고 PathGrid.DebugPath에 기록, waypoint 리스트로 변환. FindPath 내부에서만 사용.

5. ChaserAgent.cs (Astar3D, ⚠ CLAUDE.md 기준 deprecated — ChaserLocomotion + HorrorChaserAgent로 대체되어 제거 예정)
   GOAP 없이 순수 A\* 길찾기만 확인하려고 만든 테스트용 추격자 이동 스크립트. 실제 게임 로직에서는 쓰이지 않는다.
   Awake : Rigidbody, Pathfinder 참조 캐싱.
   Update : 재계산 타이머와 타겟 이동량을 검사해 RequestPath 호출 여부 결정.
   FixedUpdate : 매 물리 프레임 FollowPath 호출.
   RequestPath : Pathfinder.FindPath로 새 경로를 계산. Update에서 참조.
   FollowPath : 경로를 waypoint 단위로 따라가며 Rigidbody를 이동/회전. FixedUpdate에서 참조.

── Detection ──

6. Sound.cs
   Sound 클래스 : 소리 하나의 데이터(이름/반경/지속시간/유발 경계 단계/차원 관통 여부)만 담는 값 객체. 행동이 없고 데이터만 다르므로 상속 대신 값 채우기 방식(Goal과 같은 설계 원칙).
   SoundList 클래스 : Walking/Running/Generator/Decoy 등 게임에서 쓰는 모든 소리를 static readonly Sound로 미리 정의. Walking·Running은 PlayerStep.Update, Generator는 Object/Generator.cs의 Update에서 참조. Decoy는 아직 참조하는 곳 없음(미끼 아이템용으로 예약).

7. SoundEmitter.cs
   특정 위치에서 소리를 실제로 "방출"하는 정적 유틸리티 클래스.
   Emit : 위치·Sound 데이터·감지 대상 레이어를 받아 Physics.OverlapSphereNonAlloc으로 반경 안에 enemyLayer 오브젝트가 있는지 검사하고, 있으면 SquadBlackboard.ReportSound를 호출. PlayerStep.Update(발소리), Object/Generator.cs의 Update(발전기 소리)에서 참조.

8. VisionCensor.cs
   적의 시야 감지(거리→각도→차폐 3단계)를 수행하고 결과를 SquadBlackboard에 보고하는 컴포넌트.
   Update : detectInterval 주기로 DetectVision 실행.
   IsInViewDistance : 플레이어가 시야 거리 안에 있는지 XZ 평면 거리로 판정. DetectVision 내부에서 참조.
   IsInViewAngle : 플레이어가 시야각(viewAngle) 안에 있는지 판정. DetectVision 내부에서 참조.
   HasLineOfSight : 눈높이(eyeHeight)에서 플레이어 몸통(playerHeight)으로 레이캐스트해 장애물 유무 판정. DetectVision 내부에서 참조.
   DetectVision : 위 세 조건을 모두 만족하면 "봤다"고 판단. 보이면 SquadBlackboard.ReportSighting을 매번 호출해 위치를 갱신하고, 직전엔 보였는데 지금 안 보이면 SquadBlackboard.ReportLostSight를 한 번만 호출(상태 전환 감지, visibleJustBefore로 기억). Update에서 참조.
   OnDrawGizmosSelected : Scene 뷰에서 시야 거리/각도/레이를 시각화.

── 최상위(Assets 루트) ──

9. Follow.cs
   카메라가 target(플레이어)을 일정 offset을 두고 따라다니게 하는 컴포넌트.
   LateUpdate : target.position + offset으로 카메라 위치를 매 프레임 늦게 갱신. Update에서 갱신되는 target보다 나중에 실행되어 카메라 떨림을 방지.

10. InteractionPrompt.cs
    화면에 뜨는 상호작용 안내 문구("[E] 발전기 작동" 등)를 관리하는 씬 싱글톤. 여러 상호작용 오브젝트가 공용으로 Show/Hide를 호출한다.
    Awake : Instance 등록 후 HideImmediate로 초기 숨김.
    Show : owner와 문구를 받아 텍스트를 표시. Object/Generator.cs의 ShowPromptIfNeeded에서 참조.
    Hide : 호출한 owner가 마지막으로 띄운 주인일 때만 숨김. Object/Generator.cs의 HidePrompt에서 참조.
    HideImmediate : owner 검사 없이 즉시 숨김. Awake와 Hide 내부에서 참조.

11. Player.cs
    CharacterController 기반으로 플레이어의 이동/점프/회전을 담당하는 컨트롤러. 쿼터뷰 카메라 각도(45도)에 맞춰 입력 방향을 보정한다.
    Awake : CharacterController·Animator 참조 캐싱, 카메라 기준 forward/right 벡터를 미리 계산해 둔다.
    Update : 매 프레임 GetInput → Move → Turn 순서로 실행.
    GetInput : 방향키/Walk버튼/점프 입력을 읽는다. Update에서 참조.
    Move : 입력값과 카메라 축으로 수평 이동 벡터 계산, 점프 시 초기 y속도 부여, CharacterController.Move로 실제 이동, 착지/공중 여부에 따라 중력 처리, Animator 파라미터(isRun/isWalk/isInTheAir) 갱신. Update에서 참조.
    Turn : 이동 방향을 바라보도록 transform.LookAt으로 회전. Update에서 참조.

12. PlayerStep.cs
    플레이어가 움직이는 동안 Animator의 상태(isRun/isWalk)를 읽어 주기적으로 발소리를 방출하는 컴포넌트. Player.cs가 애니메이션을 위해 이미 계산해 둔 값을 재사용해 같은 판단을 두 번 하지 않는다.
    Awake : Animator 참조가 비어 있으면 자식에서 찾고, 파라미터 이름을 해시로 미리 변환.
    Update : 멈춰 있으면 타이머만 리셋하고, 움직이면 걷기/뛰기에 따라 다른 Sound(SoundList.Walking/Running)와 간격(walkInterval/runInterval)으로 SoundEmitter.Emit을 호출.

── Goap/Basic (GOAP 엔진 + 공용 데이터) ──

13. ChaserContext.cs
    GOAP Action과 Planner가 공통으로 참조하는 정보 묶음(추격자 자신, 블랙보드, 플레이어, 이동 헬퍼, CatchRadius/ArriveRadius 등 판정 반경)을 하나로 묶은 데이터 전달용 클래스. SquadAgent.Start와 HorrorChaserAgent.Start가 각자 자신만의 ChaserContext를 만들어 매 Action.Perform 호출 시 함께 넘긴다.

14. Goap.cs
    GOAP 엔진의 핵심 4개 클래스를 담은, 게임 로직(fact 이름 등)을 전혀 모르는 범용 엔진 파일.
    WorldState 클래스 (bool Dictionary인 Facts로 세계 상태를 표현) :
    Clone : Facts를 복사한 새 WorldState 반환. GoapPlanner.Plan 내부에서 다음 상태를 만들 때 참조.
    Matches : goal의 모든 fact가 자신의 Facts와 일치하는지 검사. GoapPlanner.Plan(전제조건/목표 달성 검사), HorrorChaserAgent.SelectGoal, SquadAgent.SelectGoal에서 참조.
    Apply : Action의 Effects를 자신의 Facts에 덮어써 병합. GoapPlanner.Plan 내부에서 참조.
    DistanceTo : goal과 다른 fact 개수(admissible 휴리스틱)를 반환. GoapPlanner.Plan 내부에서 PlanNode.HCost 계산에 참조.
    GoapAction 추상 클래스 : 이름/전제조건/효과를 갖고 GetCost·CheckProceduralPrecondition·Perform은 하위 클래스가 구현. Goap/SingleChaser/ChaserActions.cs와 Goap/MultipleChaser/Actions.cs의 각 Action 클래스들이 상속.
    Goal 클래스 : 이름·원하는 상태·우선순위만 담는 순수 데이터(코드 없어 상속 불필요). Goap/SingleChaser/ChaserGoals.cs, Goap/MultipleChaser/Actions.cs의 GoalSet에서 값을 채워 생성.
    GoapPlanner 정적 클래스 (WorldState를 노드로, Action을 엣지로 하는 상태공간 A*) :
    Plan : WorldState 공간에서 A* 탐색을 돌려 목표 상태에 도달하는 Action 순서(Queue)를 반환. HorrorChaserAgent.Replan, SquadAgent.Replan에서 참조.
    BuildPlan : 탐색이 끝난 PlanNode에서 Parent를 거슬러 올라가며 Action Queue를 구성. Plan 내부에서만 참조.

15. SquadBlackboard.cs
    씬 싱글톤. 모든 추격자가 함께 읽고 쓰는 "공유 기억"(경계 단계, 플레이어 시야/위치 정보, 소리 정보, 다중 추격자 역할 조율)을 담는다. 한 추격자의 감지가 여기 기록되면 다른 모든 추격자의 GOAP 판단에 즉시 반영된다.
    Awake : 싱글톤 인스턴스 등록(중복 인스턴스면 자신을 파괴).
    Update : 매 프레임 TimeSinceLastSeen·TimeInAlertState를 누적하고, 일정 시간(AlertedToSuspiciousTime/SuspiciousToCalmTime)이 지나면 경계 단계를 자동으로 낮춘다(Alerted→Suspicious→Calm).
    SetAlert : 경계 단계를 바꾸고 TimeInAlertState를 초기화. Update, ReportSighting, ReportSound 내부에서 참조.
    ReportSighting : 플레이어를 시야에서 발견했을 때 위치를 갱신하고 무조건 Alerted로 격상. Detection/VisionCensor.cs의 DetectVision에서 참조.
    ReportLostSight : 플레이어가 시야에서 사라졌을 때 PlayerCurrentlyVisible만 false로(LastPlayerPosition은 수색 단서로 유지). Detection/VisionCensor.cs의 DetectVision에서 참조(상태 전환 시 1회만).
    ReportSound : 소리를 들었을 때, 소리의 Alert 단계가 현재보다 높으면 격상하고 같으면 타이머만 유지, 낮으면 무시. HasSound·LastSoundPosition·SoundSource 갱신. Detection/SoundEmitter.cs의 Emit에서 참조.
    ClearSound : 소리 조사를 마쳤을 때 HasSound를 false로. Goap/SingleChaser/ChaserActions.cs의 MoveToSound·SearchSoundArea에서 참조.
    TryClaimRole / ReleaseRole : 다중 추격자가 매복 등 역할을 겹치지 않게 선점/반납. Goap/MultipleChaser/Actions.cs의 MoveToChokepointAction에서 참조(현재는 다중 추격자 데모에서만 사용).

── Goap/MultipleChaser (구버전 협력 데모, 다중 추격자 부활 대비용으로 보존) ──

16. Actions.cs
    GoalSet 정적 클래스 :
    ForPersonality : 성격(personality)에 따라 KillPlayer(10)·FindPlayer(5)·Patrol(1) 세 Goal 리스트를 생성해 반환. SquadAgent.SelectGoal에서 참조.
    ActionFactory 정적 클래스 :
    BuildActions : 모든 성격이 공유하는 다섯 Action(Chase/Chokepoint/SearchLastKnown/Attack/Patrol) 리스트를 생성. SquadAgent.Start에서 참조.
    ChasePlayerAction : 플레이어(playerVisible)에게 직접 접근해 nearPlayer를 만족. Aggressive 성격이면 비용이 싸 우선 선택된다.
    MoveToChokepointAction : 매복 지점으로 이동. CheckProceduralPrecondition에서 SquadBlackboard.TryClaimRole로 매복 역할을 선점해야 실행 가능(선점 실패 시 이 Action은 계획에서 제외됨). Ambusher 성격이면 비용이 싸다.
    SearchLastKnownAction : 마지막으로 목격된 위치(hasLastKnownPos)를 수색. Searcher 성격이면 비용이 싸다.
    AttackAction : nearPlayer 상태에서 플레이어를 공격해 playerDead를 만족(현재는 즉시 완료 처리만 하는 자리표시자).
    PatrolAction : 전제조건 없이 patrolled를 만족(현재는 즉시 완료 처리만 하는 자리표시자).
    이 파일의 핵심은 "같은 Action 목록이라도 personality별 GetCost가 달라 GoapPlanner가 서로 다른 계획을 세우게 된다"는 것 — 같은 두뇌, 다른 비용표.

17. SquadAgent.cs
    성격(Aggressive/Ambusher/Searcher)을 가진 협력형 추격자의 GOAP 두뇌. HorrorChaserAgent의 구버전으로, 다중 추격자 협력 데모용으로 남겨 두었다.
    Start : ChaserContext 생성, ActionFactory.BuildActions로 자신의 Action 목록 구성.
    Update : 계획이 없고 진행 중인 Action도 없으면 Replan, 현재 Action이 있으면 Perform 실행 후 완료되면 다음 Action으로 진행.
    Replan : BuildWorldState → SelectGoal → GoapPlanner.Plan 순서로 새 계획을 세운다. Update 내부에서 참조.
    BuildWorldState : SquadBlackboard의 정보를 바탕으로 이 개체 시점의 WorldState를 구성(다른 추격자의 감지 결과도 여기로 흘러들어온다). Replan 내부에서만 참조.
    SelectGoal : 아직 달성되지 않은 Goal 중 우선순위가 가장 높은 것을 선택. Replan 내부에서만 참조.
    ChaserPersonality enum : Aggressive/Ambusher/Searcher. Actions.cs의 비용 분기 조건으로 참조.

── Goap/SingleChaser (현재 실제로 쓰이는 단일 추격자 두뇌) ──

18. ChaserActions.cs
    ChaserActions 정적 클래스 :
    Build : ReachPlayer·MoveToSound·SearchSoundArea·WanderStep 네 Action 인스턴스를 리스트로 반환. HorrorChaserAgent.Start에서 참조.
    ReachPlayer : playerVisible일 때 플레이어에게 접근, ChaserLocomotion.MoveTo로 CatchRadius 안까지 들어오면 완료(playerCaught).
    MoveToSound : heardSound일 때 SquadBlackboard.LastSoundPosition으로 이동, 도착하면 완료(atSoundLocation). 경로가 없으면(HasValidPath false) 방어적으로 SquadBlackboard.ClearSound를 호출하고 완료 처리.
    SearchSoundArea : atSoundLocation 상태에서 SearchDuration(3초) 동안 ChaserLocomotion.LookAround로 주변을 둘러보다 완료(soundInvestigated), 완료 시 SquadBlackboard.ClearSound 호출.
    WanderStep : 전제조건 없이 항상 실행 가능. ChaserLocomotion.GetWanderTarget으로 목표를 얻어 이동하고, 도착하면 WanderPauseMin~WanderPauseMax 사이 무작위 시간 동안 대기(LookAround)한 뒤 완료 처리(wandering).

19. ChaserGoals.cs
    단일 추격자가 갖는 세 Goal(CatchPlayer=100, InvestigateSound=50, Wander=1)의 이름·우선순위·원하는 상태를 정의하는 정적 클래스. 싸울 수 없는 호러 추격자라 생존/후퇴 목표는 없음.
    Build : 세 Goal 리스트를 생성해 반환. HorrorChaserAgent.Start에서 참조.

20. ChaserLocomotion.cs
    Astar3D.Pathfinder를 감싸서 GOAP Action이 "어디로 이동해라"만 알면 되도록 해주는 이동 헬퍼. GOAP(무엇을 할지 결정하는 상태공간 A*)와 그리드 A*(어떻게 갈지) 사이를 잇는 다리.
    Awake : Rigidbody, Pathfinder 참조 캐싱.
    MoveTo : 목표 지점·도착 반경·현재 경계 단계를 받아 도착 여부를 판정하고, 필요하면 경로를 (재)계산한 뒤 FollowPath 호출. 경계 단계에 따라 이동 속도가 달라진다(MoveSpeedFor). Goap/SingleChaser/ChaserActions.cs의 ReachPlayer·MoveToSound·WanderStep에서 참조.
    MoveSpeedFor : 경계 단계(Calm/Suspicious/Alerted)에 따른 이동 속도를 반환. MoveTo 내부에서만 참조.
    LookAround : 제자리에서 서서히 회전하며 주변을 둘러본다. Goap/SingleChaser/ChaserActions.cs의 SearchSoundArea·WanderStep에서 참조.
    GetWanderTarget : 자신 주변의 무작위 walkable 지점을 최대 10회 시도해 찾아 반환(이미 있으면 재사용). Goap/SingleChaser/ChaserActions.cs의 WanderStep에서 참조.
    ClearWanderTarget : 배회 목표를 초기화해 다음 호출 때 새로 뽑도록 함. Goap/SingleChaser/ChaserActions.cs의 WanderStep에서 참조.
    HasValidPath : 유효한 경로가 있는지 여부(교착 상태 방어용). Goap/SingleChaser/ChaserActions.cs의 MoveToSound·WanderStep에서 참조.
    FollowPath : 계산된 경로를 waypoint 단위로 따라 Rigidbody를 이동/회전. MoveTo 내부에서만 참조.
    StopHorizontal : 수평 속도를 0으로. MoveTo(도착 시)와 LookAround 내부에서 참조.
    Flat : Vector3를 XZ 평면 값(y=0)으로 치환하는 보조 함수. 파일 내 여러 함수에서 공용으로 참조.

21. HorrorChaserAgent.cs [RequireComponent(ChaserLocomotion)]
    현재 실제로 쓰이는 단일 호러 추격자의 GOAP 두뇌. CatchPlayer·InvestigateSound·Wander 세 목표만 가지며, replanInterval마다 재계획하고 매 프레임 현재 Action을 진행시킨다.
    Awake : ChaserLocomotion 참조 캐싱.
    Start : ChaserContext 구성, ChaserActions.Build·ChaserGoals.Build로 Action/Goal 목록 로드.
    Update : 재계획 타이머를 관리하다 조건이 되면 Replan 호출, 현재 Action이 있으면 Perform 실행 후 완료되면 DoNextAction으로 다음 Action을 꺼냄.
    Replan : BuildWorldState → SelectGoal → GoapPlanner.Plan 순으로 새 계획을 세우고 DoNextAction으로 첫 Action을 꺼낸다. Update 내부에서 참조.
    BuildWorldState : SquadBlackboard 정보를 바탕으로 이 추격자 시점의 WorldState를 구성. Replan 내부에서만 참조.
    SelectGoal : 아직 달성되지 않았고 GoalIsRelevant를 통과한 Goal 중 우선순위가 가장 높은 것을 선택. Replan 내부에서만 참조.
    GoalIsRelevant : Goal을 지금 좇을 이유가 있는지 거르는 필터(예: 플레이어가 안 보이면 CatchPlayer는 애초에 무의미). SelectGoal 내부에서만 참조.
    Fact : WorldState에서 bool 값을 안전하게 꺼내는 보조 함수. GoalIsRelevant 내부에서만 참조.
    DoNextAction : 계획 Queue에서 다음 Action을 꺼내 \_currentAction에 저장. Update, Replan 내부에서 참조.
    OnDrawGizmos : CatchRadius와 마지막 소리 위치를 Scene 뷰에 시각화.

── Object ──

22. Generator.cs
    플레이어가 상호작용해 켤 수 있는 발전기. 켜져 있는 동안 주기적으로 소리를 방출해 "진행을 위해 켜야 하지만 켜면 들킨다"는 핵심 긴장을 만든다. 범위에 들어오면 InteractionPrompt로 안내 문구도 띄운다.
    Start : IsActive를 startsActive 초기값으로 설정.
    Update : 범위 안 + 아직 안 켜짐 + interactKey 입력이면 Activate 호출. 켜진 상태면 emitInterval마다 SoundEmitter.Emit으로 발전기 소리(SoundList.Generator) 방출.
    OnTriggerEnter / OnTriggerExit : 플레이어(playerLayer)가 상호작용 범위에 들고 날 때 \_playerInRange를 갱신하고 ShowPromptIfNeeded/HidePrompt를 호출.
    ShowPromptIfNeeded : 아직 안 켜졌다면 InteractionPrompt.Instance.Show 호출. OnTriggerEnter, Deactivate 내부에서 참조.
    HidePrompt : InteractionPrompt.Instance.Hide 호출. OnTriggerExit, Activate 내부에서 참조.
    IsInLayerMask : 레이어가 LayerMask에 포함되는지 비트 연산으로 검사. OnTriggerEnter/OnTriggerExit 내부에서만 참조.
    Activate : 발전기를 켜고 타이머를 초기화, 안내 문구를 숨긴다. Update에서 참조.
    Deactivate : 발전기를 끄고, 플레이어가 아직 범위 안이면 안내 문구를 다시 띄운다. 현재 이 스크립트 밖에서 호출하는 코드는 없음(외부 트리거용으로 열어 둔 공개 API).

소리가 발생되는 과정
(처음 듣는 소리)
각 오브젝트의 스크립트가 Emit 호출
Emit 함수는 ReportSound 호출
ReportSound는 이미 조사된 소리가 아니라면 board를 업데이트하여 적이 조사하게끔 유발
적은 조사한 후 AddInvestigateCompleted 호출
AddInvestigateCompleted는 board를 업데이트하여 조사된 소리 관리
(이미 들었던 소리)
Emit 호출
ReportSound 호출
ReportSound는 조사된 소리를 받았으므로 아무것도 하지 않음

차원 이동 구현
차원은 AlertLevel과 같이 각자 고유한 데이터를 가진 것이 아닌, 이름표일 뿐이므로 enum으로 손쉽게 정의 및 관리가 가능.

만들 조각들

1. 차원 표시 — Dimension enum + DimensionMember (완료)

2. 플레이어의 차원 — Player에 상태 추가 (완료)

3. 감지 관문 — VisionCensor

지금 거리·각도·차폐 세 조건을 &&로 엮고 있죠. 거기에 "같은 차원인가"를 하나 더 넣습니다.

앞서 정한 대로 가장 싼 검사를 앞에 두면 되고요.

4. 보이는 것 — 렌더러 켜고 끄기

플레이어 차원이 바뀔 때, 다른 차원 오브젝트의 렌더러를 끕니다. 로직은 살려두고 그리기만 멈추는 거죠.

이걸 누가 관리할지가 판단 지점입니다. 각 DimensionMember가 스스로 판단할 수도 있고, 어딘가 관리자가 일괄 처리할 수도 있어요.

5. 전환 존

발전기와 거의 같은 구조입니다. 트리거 콜라이더, InteractionPrompt, 키 입력. 앞서 만든 걸 그대로 재사용할 수 있죠.

1차 작업

새로 생성된 파일

DimensionController : 빈 오브젝트를 만들어 컴포넌트로 추가.
enum 형식으로 두 차원을 정의한다.
플레이어와 적들의 차원 정보를 Awake에서 받아놓는다.
Portal이 차원 변경을 알리면, 플레이어의 차원을 바꿔준 뒤
각각의 적들의 MeshRenderer.enabled 정보를 foreach문 안에서 switch해준다.

DimensionMember : 적 오브젝트에 컴포넌트로 추가.
차원을 초월하지 못하고, 넘나들지도 못하는,
차원 정보가 고정되어 있는 적에게 붙이는 딱지같은 개념.
DimensionController가 이 딱지를 보고, 플레이어와 같은 차원에 있는 적만
MeshRenderer를 켜고, 다른 차원에 있는 적은 MeshRenderer를 꺼서 보이지 않게 한다.

Portal : 포탈 오브젝트에 컴포넌트로 추가.
DimensionController를 멤버변수로 가진다.
플레이어가 다가와 키를 누르면 컨트롤러에게 알린다 (함수 호출)

변경된 기존 파일

Player :
플레이어가 가진 차원을 바꾸는 함수 추가
호출하는 주체는 컨트롤러.

VisionCensor :
차원을 검사하는 절차를 맨 먼저 밟도록 수정해야 함. 어떻게 수정할까?

2차 작업

문제 1 (완)
DimensionController를 싱글톤으로 만들고
컨트롤러를 참고하는 Portal, VisionCensor에서 Instance를 호출하도록 수정해야 함

문제 2 (완)
컨트롤러가 플레이어의 차원을 바꿀 때 불필요한 dimension 필드가 사용되고 있음
컨트롤러가 이 필드를 가질 필요가 있을까?
-> Player의 SwitchMyDimension을 void로 바꿈. 컨트롤러에서는 따로 자신의 필드에 무언가를 받지 않고 플레이어에 명령만 내림.

문제 3
게임이 시작됐을 때 렌더러들의 정리가 필요함. 지금은 차원이 바뀔 때만 렌더러가 정리되고 있음

문제 4 (완)
적의 렌더러를 바꾸는 것을 컨트롤러가 직접 할 필요가 있을까? member에서 알아서 하라고 하면 될 듯

문제 5 (완)
IsInSameDimension 구현. 컨트롤러에게 차원 정보들을 받아오는 편이 좋을 듯
-> Transform만으로는 알 수 없기에 parameter는 따로 두지 않음.
GetComponent로 해당 적 개체의 member를 가져와서, 컨트롤러에 비교해달라고 요청
컨트롤러는 플레이어의 차원과 (get; private set; 을 통해 컨트롤러에서도 알 수 있음)
요청받은 member의 차원을 비교하여 bool 값을 센서에 전달

3차 작업
DimensionMember.ToggleRenderer()이 Renderer 자기 자신만 보고 켜고 끄고 있다
-> 컨트롤러에 있는 Compare 함수 활용!

enemies가 public이어어서 캡슐화 관점에서 아쉬운 요소.
-> private으로 바꾸고 컨트롤러에 멤버를 추가하는 public 함수 추가

IsInSameDimension이 Update마다 GetComponent하는 문제
-> GetComponent는 Censor의 Start에서 한번만 하는 것으로 수정

4차 작업
현재 구조가
    Controller → Member (enemies에 담고 있음)
    Member → Controller (CompareDimension 호출)
서로를 참조하는 구조 (되도록이면 피해야 할 점)
대안 -> SetRenderer가 판단 결과를 받도록!
컨트롤러가 비교하여 너 나타나/숨어 를 지시
멤버는 받은 대로 렌더러만 처리
요지는, 멤버가 컨트롤러를 모르게!

Q: 내가 적의 WorldState에 대해서 제대로 이해하고 있는게 맞는지 모르겠는데, 게임이 시작되고 나서 적의 State는 비어있는 상태고, 소리를 한번이라도 듣는 순간 HeardSound 키가 생기고, 플레이어를 한번이라도 보는 순간 PlayerVisible이 생기는 구조 맞나?

A: 아니요, 그렇지 않습니다. 매 재계획마다 새로 만들어집니다.
실제 흐름
BuildWorldState가 하는 일을 보면 명확합니다.

Replan() 호출 (0.4초마다)
  → BuildWorldState()
      새 WorldState 객체 생성
      블랙보드를 읽어서 fact들을 채움
  → 이 상태로 계획 수립

매번 새 WorldState를 만들고, 그 시점의 블랙보드를 스냅샷처럼 찍는 겁니다.

키는 처음부터 있습니다

BuildWorldState는 조건 없이 모든 fact를 채웁니다.

playerVisible = (블랙보드 값)     ← true든 false든 항상 넣음
heardSound    = (블랙보드 값)     ← 마찬가지

그러니 게임 시작 직후에도 키는 다 있습니다. 값이 false일 뿐이죠.

게임 시작
→ WorldState: { playerVisible: false, heardSound: false, ... }

"소리를 들으면 키가 생긴다"가 아니라, 키는 항상 있고 값이 바뀌는 구조입니다.

누적되지 않습니다

이게 중요한 지점입니다. WorldState는 기억이 아닙니다.

0.0초: WorldState 생성 → { playerVisible: true }  → 사용 후 버림
0.4초: WorldState 생성 → { playerVisible: false } → 사용 후 버림
0.8초: WorldState 생성 → { playerVisible: false } → ...

이전 상태를 이어받지 않고, 매번 처음부터 만듭니다.

기억을 담당하는 건 블랙보드죠. 블랙보드가 PlayerCurrentlyVisible, HasSound, Alert 같은 걸 계속 들고 있고, WorldState는 그걸 매번 복사해 오는 겁니다.

그럼 WorldState는 왜 있나

*GOAP 플래너가 A 탐색을 하기 위한 재료**입니다.

앞서 다룬 상태공간 A*를 떠올려보세요.

현재 상태 → [행동 적용] → 새 상태 → [행동 적용] → ... → 목표 상태

플래너는 이 탐색 과정에서 가상의 상태들을 계속 만들어냅니다.

{ playerVisible: true, playerCaught: false }
  → ReachPlayer 적용 (Effects: playerCaught = true)
{ playerVisible: true, playerCaught: true }  ← 이건 상상 속의 상태
  → 목표와 일치? → 계획 완성

두 번째 상태는 실제로 일어난 게 아닙니다. "이 행동을 하면 이렇게 될 것"이라는 가정이죠.

그래서 WorldState에 Clone()과 Apply()가 있는 겁니다. 원본을 복제해서 효과를 적용해보는 거죠.

정리하면 역할이 다릅니다
	블랙보드	WorldState
성격	지속되는 기억	일회용 스냅샷
수명	게임 내내	재계획 한 번
누가 씀	감지가 쓰고 GOAP가 읽음	플래너가 탐색에 사용
내용	실제 상황	실제 + 가상

BuildWorldState는 둘 사이의 번역기입니다. "블랙보드의 언어"를 "플래너가 이해하는 fact"로 옮기는 거죠.

B 방식 구현에 이게 왜 중요한가

지금 하시려는 작업이 정확히 이 번역 지점에 손대는 겁니다.

지금:    블랙보드 값을 그대로 옮김
바꾸면:  옮기면서 "나에게 유효한가"를 거름

WorldState가 매번 새로 만들어지니, 필터가 매 재계획마다 다시 적용됩니다. 차원이 바뀌면 다음 재계획(0.4초 이내)에 반영되죠.

한 번 걸러진 게 남아 있을 걱정은 없습니다. 매번 새로 만드니까요.




Q: private static bool Fact(WorldState s, string key)
            => s.Facts.TryGetValue(key, out bool v) && v;
그러면 GoalIsRelevant 함수에 쓰이는 이 Fact 함수는 s에 key가 있는지를 먼저 검사하고 그 다음에 key의 value가 true인지를 검사하는 거 아냐? key가 존재하지 않을 일이 없다면 key가 있는지 검사는 왜 하는 거지?

A: 좋은 지적입니다. 지금 BuildWorldState가 만든 상태에 대해서는 그 검사가 불필요합니다. 모든 키를 항상 채우니까요.

그런데 이 함수가 그렇게 생긴 이유가 있습니다.

WorldState는 다른 데서도 만들어집니다
앞서 얘기했듯, 플래너가 탐색하면서 가상의 상태를 계속 만들죠. 그리고 Goal의 Desired도 WorldState입니다.

csharp
// ChaserGoals에서
catchPlayer.Desired.Facts["playerCaught"] = true;   // 키가 하나뿐
목표는 "원하는 것"만 담습니다. playerVisible이나 heardSound는 안 들어 있죠. 앞서 Matches가 "goal의 fact만 검사한다"고 했던 게 이 때문입니다.

이런 WorldState에 Fact(goal.Desired, "heardSound")를 부르면 키가 없습니다.

없는 키를 그냥 읽으면
csharp
s.Facts["heardSound"]   // KeyNotFoundException 발생!
Dictionary는 없는 키를 읽으면 예외를 던집니다. 게임이 멈추죠.

TryGetValue는 그걸 막고 "없으면 false"로 처리합니다.

의미상으로도 맞습니다
GOAP에서 "키가 없다"와 "false"는 같은 뜻으로 취급하는 게 자연스럽습니다.

heardSound가 없다  →  소리를 못 들었다  →  false
"모르는 것은 참이 아니다"라는 규칙이죠. 이걸 closed-world assumption이라고 부릅니다. 자동 계획 분야에서 표준적인 가정입니다.

그래서 이 한 줄이 두 가지를 동시에 합니다.

예외 방지 (기술적)
"없음 = 거짓" 규칙 표현 (의미적)
앞서 다룬 것과 같은 맥락
WorldState.Matches를 떠올려보세요.

goal의 fact만 검사, 나머지는 무시
goal에 없는 fact는 "상관없다"는 뜻이었죠. 여기 Fact도 같은 관용성을 갖는 겁니다.

엔진이 특정 fact 집합을 전제하지 않는다는 설계예요. 앞서 "Goap.cs는 fact 이름을 모르는 범용 엔진"이라고 한 것과 이어집니다.

그래서 지금은 불필요하지만
말씀하신 대로, BuildWorldState가 만든 상태에서는 키가 항상 있으니 TryGetValue의 첫 검사가 매번 통과합니다.

하지만 이 함수는 어떤 WorldState가 들어와도 안전하게 만들어졌습니다. 나중에 이런 일이 생겨도 안 깨지죠.

fact를 조건부로만 채우는 경우
플래너가 만든 중간 상태를 검사하는 경우
goal의 Desired를 검사하는 경우
성능은 무시할 수준
혹시 그게 걱정이시라면, TryGetValue는 []와 거의 같은 비용입니다. 해시로 한 번 찾아보고 있으면 값을, 없으면 false를 반환하는 것뿐이죠.

오히려 ContainsKey로 확인한 뒤 []로 읽으면 두 번 찾게 되어 느립니다. TryGetValue가 그걸 한 번에 하는 게 존재 이유고요.

정리
지금 상황에서는 방어적인 코드가 맞습니다. 다만 "왜 있는지 모르고 두는 것"과 "알고 두는 것"은 다르죠.

알고 계시면 나중에 판단할 수 있습니다. 예를 들어 "fact를 조건부로 채우도록 바꾸자"고 할 때, 이 함수가 이미 대비되어 있다는 걸 아니까요.