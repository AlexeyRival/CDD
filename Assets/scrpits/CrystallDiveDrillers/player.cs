using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class player : NetworkBehaviour
{
    public GameObject head,firstcamera,diecamera,slot1,slot10;
    public GameObject rotator, supplymarker;
    public Fog fog;
    public Camera cam;

    //оружие
    public Weapon[] weapons;
    public Weapon weapon;


    public AudioSource Audio;
    public AudioClip m_getresource;
    public float speed = 75f;
    private float sprint=1f;
    private float acceleration;
    private bool ismoving;
    private bool canJump;
    private bool canUpJump;
    public GameObject sphereDestroyer, flare;
    public GameObject[] grenades;
    public TextMesh hpobject;
    private Rigidbody rb;
    private Vector3 mousedelta;
    private float rot,yrot;
    private RaycastHit hit;
    public List<Resource> resources;
    public Animator animator;
    private Generator generator;
    private MissionMenuController missionMenuController;
    private bool isTeleported = false;
    private float attackcooldown = 0;
    private float flarecooldown = 0;
    private int localhp = 101;
    private float magnitude;
    private bool seeContainer, seeInteraction,classchangeInteraction,upgradeWeaponTerminalInteracton, gobackinteraction, missionselectorinteraction,supplyinteracton;
    private float containercooldown = 0;
    private int tick;
    private float tickf;
    private GameObject targetobject;
    private bool truestart = false;
    private customNetworkHUD network;

    //спецеффекты
    public bool camShake;


    //звуки!
    private float roomsize;

    //движение
    private float timerforcam;
    private Vector3 moveVector;
    private Vector3 oldpos;

    //след игрока для жуков
    public GameObject smell;
    private GameObject thissmell;

    //смерть
    private bool deathInteraction;
    public static bool isDead;
    private bool isReviving;
    private float prevrevivepoints;

    //оружие
    private float grenadecooldown=0;
    private int grenadecount;
    private bool scopeTrg = false;
    private float reloadtimer = 0f;
    private int ammo, allammo;

    //суплай
    public GameObject supplyprefab;
    private NetworkInstanceId supply;
    private float supplyprogress;
    private bool isTryingSupply;

    //щиты
    private int shield=100;
    private float shieldcooldown;
    private Image shieldpic;

    //инвентарь и всё прочее интерфейсное
    
    public int characterclass=-1;
    public int currentslot=0;
    public characterclass[] classes;
    public GameObject UIobject, UIInventory, UIRevive, crosshair, sniperscope, UIE, UIscaner, UISupplyCost, UIweapon, UIEscapeMenu;
    public Text UIhp,UIshld,UIammo,UIgrenadecounter;
    public Slider hpbar, shldbar, flarebar,grenadebar,compass,supplyprogressbar;
    public Image classpic,UIBlackScreen;
    public GameObject expscreen;
    private bool isFade;
    private bool isEscape,someEscapeTrigger;
    private float uiweapontimer;

    //уровни и опыт
    public Sprite[] rankpics;
    public int rank;
    public static int level;
    private int locallevel;
    public static int xp;
    public static int money;
    public static int dwarfclass=-1;
    [SyncVar]
    public int kills=0;

    //другие игроки
    public GameObject UIPlayerPrefab,UIPlayersbase;
    public static List<player> players;
    public static player thisplayer;
    private int updatetimer;
    

    //урон жукам
    public bug.SyncListDamageInfo damageinfos = new bug.SyncListDamageInfo();

    //дела отладочные
    private float FPS;


    private GameObject[] allitems;
    [SyncVar]
    public bool isDropToContainer;
    [SyncVar]
    public int hp = 100;
    [SyncVar]
    public string nickname="Player";
    [SyncVar]
    public SyncListInt resourcesCount;
    // Start is called before the first frame update

    [Command]
    void CmdSpawnDestroyer(Vector3 pos) {
        //for (int i = 0; i < 5; ++i)
        {
            GameObject ob = Instantiate(sphereDestroyer, pos, Quaternion.identity);
            Destroy(ob, 0.1f);//.1f
            NetworkServer.Spawn(ob);
        }
    }
    [Command]
    void CmdMoveMarker(Vector3 pos) {
        generator.MoveMuleMarker(pos);
    }
    [Command]
    void CmdDie() {
        generator.AddDeathPlayer(nickname);
    }
    [Command]
    public void CmdSpawnSupply(Vector3 position) 
    {
        GameObject ob = Instantiate(supplyprefab, new Vector3(position.x, 117, position.z), Quaternion.identity);
        NetworkServer.Spawn(ob);
        ob.GetComponent<supply>().SetVector(position);
        generator.resourcesCount[0] -= 40;
    }
    [Command]
    public void CmdGetSupply(NetworkInstanceId id) 
    {
        for (int i = 0; i < FindObjectsOfType<supply>().Length; ++i) 
        {
            if (id == FindObjectsOfType<supply>()[i].netId) 
            {
                --FindObjectsOfType<supply>()[i].amount;
                break;
            }
        }
    }
    [Command]
    void CmdAddRevivePoints(string name,float amout) {
        generator.AddRevivePoints(name, amout);
    }
    [Command]
    void CmdTryRevive()
    {
        if (generator.GetAlive(nickname)) {
            hp = 100;
            generator.AddRevivePoints(nickname, -100);
        }
    }
    [Command]
    void CmdSpawnFlare() {
        GameObject ob= Instantiate(flare, head.transform.position, head.transform.rotation);
        ob.transform.Translate(0, 0, 1);
        ob.GetComponent<Rigidbody>().AddRelativeForce(0,0,20,ForceMode.Impulse);
        Destroy(ob, 30f);
        NetworkServer.Spawn(ob);
    }
    [Command]
    void CmdSpawnGrenade() {
        GameObject ob= Instantiate(grenades[characterclass], head.transform.position, head.transform.rotation);
        ob.transform.Translate(0,0,1);
        ob.GetComponent<Rigidbody>().AddRelativeForce(0,0,20,ForceMode.Impulse);
        Destroy(ob, 30f);
        NetworkServer.Spawn(ob);
    }
    [Command]
    void CmdSpawnProjectile() {
        GameObject ob= Instantiate(weapon.projectile, head.transform.position, head.transform.rotation);
        ob.transform.Translate(0,0,1);
        ob.GetComponent<Rigidbody>().AddRelativeForce(0,0,20*weapon.projectilespeed,ForceMode.Impulse);
        Destroy(ob, weapon.projectileLifeTime);
        NetworkServer.Spawn(ob);
    }
    [Command]
    void CmdAddResource(int id,int amout) {
        resourcesCount[id]+=amout;
        if(!isLocalPlayer)if (amout < 0) {
            generator.AddResource(id, -amout);
        }
    }
    [Command]
    void CmdResetKills() {
        kills = 0;
    }
    [Command]
    void CmdStartMission() {
        generator.StartMission();
        //generator.StartPlatform();
    }
    [Command]
    void CmdEndMission() {
        generator.GoBack();
    }
    [Command]
    void CmdSetNick(string nick) {
        nickname = nick;
    }
    [Command]
    void CmdSetClass(int characterclass, int rank,int level) {
        this.characterclass = characterclass;
        RpcSetClass(characterclass, rank,level);
        CmdSetSlot(0);
    }
    [ClientRpc]
    void RpcSetClass(int characterclass, int rank,int level) {
        this.characterclass = characterclass;
        locallevel = level;
        PlayOneShot("event:/greatings", "Parameter 1", characterclass);
        weapon = weapons[characterclass];
        ammo = weapon.ammo;
        allammo = weapon.maxammo;
        grenadecount = grenades[characterclass].GetComponent<grenade>().maxcount;
        speed = classes[characterclass].speed;
        this.rank = rank;
        if (isLocalPlayer)
        {
            dwarfclass = characterclass;
            UIammo.text = ammo + "/" + allammo;
            UIgrenadecounter.text = grenadecount + "";
        }
    }
    [Command]
    void CmdSetSlot(int slot) {
        currentslot = slot;
        RpcSetSlot(slot);
    }
    [ClientRpc]
    void RpcSetSlot(int slot) 
    {
        currentslot = slot;
        slot1.SetActive(slot == 1);
        for (int i = 0; i < 8; ++i) 
        {
            slot1.transform.GetChild(i + 1).gameObject.SetActive(i == characterclass);
        }
        slot10.SetActive(slot == 0);
        if (isLocalPlayer)
        {
            if (slot == 1)
            {
                slot1.transform.Rotate(60, -60, -60);
                UIweapon.SetActive(true);
                uiweapontimer = 3f;
                UIweapon.transform.GetChild(1).GetComponent<Image>().sprite = weapon.icon;
                UIweapon.transform.GetChild(2).GetComponent<Text>().text = weapon.weaponname;
            }
            else 
            {
                UIweapon.SetActive(false);
                uiweapontimer = 0f;
            }
            for (int i = 0; i < 8; ++i) { 
                crosshair.transform.GetChild(i).gameObject.SetActive(slot == 1&&characterclass==i); 
            }
        }
    }
    [Command]
    void CmdDmg(int dmg) {
        hp = hp-dmg;
        if (hp > 100) { hp = 100; }
    }
    [Command]
    void CmdIsDrop(bool isdrop) {
        isDropToContainer = isdrop;
    }
    public void Dmg(int dmg) {
        if (dmg-shield < 10)
        {
            shield -= dmg;
            shieldpic.color = new Color(1, 1, 1, dmg * 0.02f);
            if (shield < 0) shield = 0;
        }
        else {
            shield = 0;
            CmdDmg(dmg-shield);
        }
        shieldcooldown = 4f;
    }
    private bool isClear = false;
    private void Clear() {
        for (int i = 0; i < resourcesCount.Count; ++i) {
            if (resourcesCount[i] > 0) { CmdAddResource(i, -resourcesCount[i]); }
        }
        CmdDmg(-(100 - hp));
        isClear = true;
    }
    public void Attack()
    {
        CmdMakeSound("event:/whoosh");
            Physics.Raycast(head.transform.position, head.transform.forward, out hit, 6);
            if (hit.transform)
            {
                CmdSpawnDestroyer(hit.point);
            }
    }
    [Command]
    private void CmdMakeSound(string soundname) 
    {
        RpcMakeSound(soundname);
    }
    [ClientRpc]
    private void RpcMakeSound(string soundname)
    {
        PlayOneShot(soundname);
    }
    [Command]
    private void CmdMakeSoundArg(string soundname, string argname, int argvalue) 
    {
        RpcMakeSoundArg(soundname,argname,argvalue);
    }
    [ClientRpc]
    private void RpcMakeSoundArg(string soundname, string argname, int argvalue)
    {
        PlayOneShot(soundname, argname, argvalue);
    }
    [Command]
    public void CmdAddInfo(NetworkInstanceId netid,int dmg) {
        damageinfos.Add(new bug.damageinfo(netid, Random.Range(int.MinValue, int.MaxValue), Time.realtimeSinceStartup, dmg));
    }
    [Command]
    private void CmdBulletMark(Vector3 point,GameObject trf, Vector3 secondpoint) 
    {
        RpcBulletMark(point,trf,secondpoint);
    }
    [ClientRpc]
    private void RpcBulletMark(Vector3 point,GameObject trf,Vector3 secondpoint) 
    {
        GameObject ob = Instantiate(weapon.bulletmark, point, slot1.transform.GetChild(0).transform.rotation);
        ob.transform.GetChild(0).GetComponent<LineRenderer>().SetPosition(0, secondpoint);
        ob.transform.GetChild(0).GetComponent<LineRenderer>().SetPosition(1, ob.transform.position);
        ob.transform.Rotate(-90, 0, 0);
        if(trf)ob.transform.parent = trf.transform;
        Destroy(ob.transform.GetChild(0).gameObject, 0.5f);
        Destroy(ob, 3f);
    }
    private void Shoot(RaycastHit hit)
    {
        if (hit.transform.gameObject.CompareTag("Bug"))
        {
            hit.transform.GetComponent<bug>().Dmg(weapon.dmg, hit.collider.gameObject);
            CmdAddInfo(hit.transform.GetComponent<NetworkIdentity>().netId, weapon.dmg);
            crosshair.transform.GetChild(characterclass).GetComponent<Image>().color = crosshair.transform.GetChild(characterclass).GetComponent<Image>().color - new Color(0, weapon.dmg * 0.1f, weapon.dmg * 0.1f, 0);
            crosshair.transform.GetChild(characterclass).GetComponent<Image>().color = new Color(1, crosshair.transform.GetChild(characterclass).GetComponent<Image>().color.g > 0 ? crosshair.transform.GetChild(characterclass).GetComponent<Image>().color.g : 0, crosshair.transform.GetChild(characterclass).GetComponent<Image>().color.b > 0 ? crosshair.transform.GetChild(characterclass).GetComponent<Image>().color.b : 0, crosshair.transform.GetChild(characterclass).GetComponent<Image>().color.a);

            if (weapon.isChain)
            {
                GameObject[] bugs = GameObject.FindGameObjectsWithTag("Bug");
                int chaincount = weapon.chaincount + Random.Range(-2, 3);
                for (int i = 0; i < bugs.Length; ++i) if (bugs[i] != hit.transform.gameObject && Vector3.Distance(hit.transform.position, bugs[i].transform.position) < 10)
                    {
                        bugs[i].GetComponent<bug>().Dmg(weapon.dmg / 2, bugs[i]);
                        CmdAddInfo(bugs[i].GetComponent<NetworkIdentity>().netId, weapon.dmg / 2);
                        CmdBulletMark(bugs[i].transform.position, bugs[i], hit.point);
                        if (chaincount > 0) { --chaincount; } else { break; }
                    }
            }
        }
        if (hit.transform.gameObject.CompareTag("Hive"))
        {
            hit.transform.GetComponent<hive>().Dmg(weapon.dmg, hit.collider.gameObject);
            CmdAddInfo(hit.transform.GetComponent<NetworkIdentity>().netId, weapon.dmg);
            crosshair.transform.GetChild(characterclass).GetComponent<Image>().color = crosshair.transform.GetChild(characterclass).GetComponent<Image>().color - new Color(0, weapon.dmg * 0.1f, weapon.dmg * 0.1f,0);
            crosshair.transform.GetChild(characterclass).GetComponent<Image>().color = new Color(1, crosshair.transform.GetChild(characterclass).GetComponent<Image>().color.g > 0 ? crosshair.transform.GetChild(characterclass).GetComponent<Image>().color.g : 0, crosshair.transform.GetChild(characterclass).GetComponent<Image>().color.b > 0 ? crosshair.transform.GetChild(characterclass).GetComponent<Image>().color.b : 0, crosshair.transform.GetChild(characterclass).GetComponent<Image>().color.a);
        }
        {
            CmdBulletMark(hit.point, hit.transform.gameObject, slot1.transform.GetChild(0).transform.position);
        }

    }
    public void Fire() {
        if (weapon.haveRotor) 
        {
            slot1.transform.GetChild(characterclass + 1).GetChild(1).Rotate(0,0,30);
        }

        CmdMakeSoundArg("event:/weaponfire", "weaponID", weapon.sound);

        Destroy(Instantiate(weapon.firesplash, slot1.transform.GetChild(0).transform.position, slot1.transform.GetChild(0).transform.rotation,slot1.transform),0.4f);
        if (weapon.haveProjectile)
        {
            CmdSpawnProjectile();
        }
        else
        {
            if (weapon.isRailgun) 
            {
                RaycastHit[] hits = Physics.RaycastAll(head.transform.position, head.transform.forward, weapon.haveScope ? 150 : 50);
                if (hits.Length > 0)
                    for (int i = 0; i < (hits.Length<weapon.railStreakCount?hits.Length:weapon.railStreakCount); ++i) {
                        Shoot(hits[i]);
                    }
            }
            else
            if (Physics.Raycast(head.transform.position, head.transform.forward, out hit, weapon.haveScope ? 150 : 50))
            {
                Shoot(hit);
            }
        }
        yrot -= Random.Range(-weapon.recoil, weapon.recoil) * 2;
        yrot = Mathf.Clamp(yrot, -90, 90);
        head.transform.localRotation = Quaternion.Euler(yrot, 0, 0);
        transform.Rotate(0, Random.Range(-weapon.recoil, weapon.recoil) * 2, 0);
        slot1.transform.Rotate(Random.Range(-weapon.recoil, weapon.recoil) * 9f, Random.Range(-weapon.recoil, weapon.recoil) * 9f, Random.Range(-weapon.recoil, weapon.recoil) * 9f);
        slot1.transform.Translate(Random.Range(-weapon.recoil, weapon.recoil) * 0.1f, Random.Range(-weapon.recoil, weapon.recoil) * 0.1f, Random.Range(-weapon.recoil, -weapon.recoil * 0.4f) * 0.2f);

    }
    private void AddPlayer(player player,int ix) {
       GameObject ui = Instantiate(UIPlayerPrefab, UIPlayersbase.transform.position, UIPlayersbase.transform.rotation, UIPlayersbase.transform);
        ui.name = player.netId.ToString();
        ui.transform.Translate(ix * 90, 0, 0);
        ui.transform.Find("Name").GetComponent<Text>().text = player.nickname;
        ui.transform.Find("HPbar").GetComponent<Slider>().value = player.hp*0.01f;
        ui.transform.Find("Level").GetComponent<Text>().text = player.locallevel.ToString();
        ui.transform.Find("Rank").GetComponent<Image>().sprite = rankpics[player.rank];
        if (player.characterclass!=-1)ui.GetComponent<Image>().sprite = classes[player.characterclass].icon;
    }
    private void UpdatePlayer(player player) {
        GameObject ui = UIPlayersbase.transform.Find(player.netId.ToString()).gameObject;
        if (ui)
        {
            ui.transform.Find("HPbar").GetComponent<Slider>().value = player.hp * 0.01f;
            ui.transform.Find("Name").GetComponent<Text>().text = player.nickname;
            ui.transform.Find("Level").GetComponent<Text>().text = player.locallevel.ToString();
            ui.transform.Find("Rank").GetComponent<Image>().sprite = rankpics[player.rank];
            if (player.characterclass != -1) ui.GetComponent<Image>().sprite = classes[player.characterclass].icon;
        }
    }
    private void RemovePlayer(player player) 
    {
        GameObject ui = UIPlayersbase.transform.Find(player.netId.ToString()).gameObject;
        bool minus = false;
        for (int i = 0; i < UIPlayersbase.transform.childCount; ++i) 
        {
            if (minus) 
            {
            UIPlayersbase.transform.GetChild(i).transform.Translate(-90, 0, 0);
            }
            if (UIPlayersbase.transform.GetChild(i).name==player.netId.ToString()) 
            {
                minus = true;
            }
        }
        Destroy(ui);
    }
    private void UpdateResources() {
        int xshift;
        int activecount=0;
        for (int i = 0; i < resourcesCount.Count; ++i) {
            UIInventory.transform.GetChild(i).gameObject.SetActive(resourcesCount[i] > 0);
            if (resourcesCount[i] > 0)
            {
                UIInventory.transform.GetChild(i).GetChild(3).GetComponent<Text>().text = resourcesCount[i].ToString()+"/"+resources[i].maxInBag;
                ++activecount;
            }
        }
        xshift = (activecount / 2)*-50;
        int iter = 0;
        for (int i = 0; i < resourcesCount.Count; ++i)
        {
            if (resourcesCount[i] > 0)
            {
                UIInventory.transform.GetChild(i).transform.localPosition = new Vector3(xshift + iter * 60, 0, 0);
                ++iter;
            }
        }
    }

    //загрузка
    [SyncVar]
    public float loadingstatus;
    [Command]
    public void CmdSetProgress(float f) {
        loadingstatus = f;
    }
    private void Start()
    {
        UIPlayersbase = GameObject.Find("Canvas").transform.Find("UI").transform.Find("PlayersBase").gameObject;
        if (isServer) for (int i = 0; i < resources.Count; ++i) { resourcesCount.Add(0); }
        generator = GameObject.Find("ChungGenerator").GetComponent<Generator>();
        missionMenuController = GameObject.Find("MissionMenuController").GetComponent<MissionMenuController>();
        if (!isLocalPlayer) { head.transform.GetChild(0).gameObject.SetActive(false); }
        rb = GetComponent<Rigidbody>();
        if (players == null) players = new List<player>();
        if (!isLocalPlayer)
        {
            MissionControlVoice.only.PlayReplica(1);
            AddPlayer(this, players.Count);
            players.Add(this);
        }
        else 
        {
            thisplayer = this;
        }
        if (isServer)
        {
            thissmell = Instantiate(smell, transform.position, transform.rotation);
        }
        for (int i = 0; i < GameObject.FindGameObjectsWithTag("Player").Length;++i) {
            GameObject.Find("network").GetComponent<customNetworkHUD>().spawnpoints[i].transform.parent.parent.GetComponent<Animation>().Play(); 
        }
    }
    void TrueStart()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        network = GameObject.Find("network").GetComponent<customNetworkHUD>();
        CmdSetNick(network.nickname);
        rank = network.dwarfprestige[network.characterclass] > 10 ? 4 : (network.dwarfprestige[network.characterclass] > 0 ? 3 : (network.dwarfeliteranks[network.characterclass] > 0 ? network.dwarfeliteranks[network.characterclass] : 0));
        CmdSetClass(network.characterclass, rank,levels[network.characterclass]);
        CmdDmg(1);
        hpobject.gameObject.SetActive(false);
        UIobject = GameObject.Find("Canvas").transform.Find("UI").gameObject;
        UIobject.SetActive(true);
        UIhp = UIobject.transform.Find("HeartImage").transform.Find("HPText").GetComponent<Text>();
        hpbar = UIobject.transform.Find("HeartImage").transform.Find("HealthBorder").GetComponent<Slider>();
        UIshld = UIobject.transform.Find("ShieldImage").transform.Find("ShieldText").GetComponent<Text>();
        shldbar = UIobject.transform.Find("ShieldImage").transform.Find("ShieldBorder").GetComponent<Slider>();
        flarebar = UIobject.transform.Find("Flare").GetComponent<Slider>();
        compass = UIobject.transform.Find("Compass").GetComponent<Slider>();
        supplyprogressbar = UIobject.transform.Find("Supply").GetComponent<Slider>();
        grenadebar = UIobject.transform.Find("Grenade").GetComponent<Slider>();
        UIammo = UIobject.transform.Find("Ammo").GetComponent<Text>();
        UIgrenadecounter = UIobject.transform.Find("GrenadeCounter").GetComponent<Text>();
        classpic = UIobject.transform.Find("ClassBorder").GetComponent<Image>();
        shieldpic = UIobject.transform.Find("Shield").GetComponent<Image>();
        UIBlackScreen = GameObject.Find("Canvas").transform.Find("Blackscreen").GetComponent<Image>();
        classpic.sprite = classes[GameObject.Find("network").GetComponent<customNetworkHUD>().characterclass].icon;
        UIInventory = UIobject.transform.Find("Inventory").gameObject;
        UIRevive = UIobject.transform.Find("Revive").gameObject;
        crosshair = UIobject.transform.Find("AIM").gameObject;
        sniperscope = UIobject.transform.Find("SniperScope").gameObject;
        UIscaner = UIobject.transform.Find("Scaner").gameObject;
        UIE = UIobject.transform.Find("E").gameObject;
        UISupplyCost = UIobject.transform.Find("SupplyCost").gameObject;
        UIweapon = UIobject.transform.Find("Weapon").gameObject;
        UIEscapeMenu = GameObject.Find("Canvas Menu").transform.Find("Escape_menu").gameObject;
        UIEscapeMenu.transform.Find("ExitMission").GetComponent<Button>().onClick.AddListener(() => { EscapeMission();isEscape = false;UIEscapeMenu.SetActive(false);Cursor.lockState = CursorLockMode.Locked;Cursor.visible = false; });
        UIEscapeMenu.transform.Find("ExitGame").GetComponent<Button>().onClick.AddListener(() => { Destroy(gameObject); });
    }
    private bool quitting;
    private void OnApplicationQuit()
    {
        quitting = true;
    }
    private void OnDestroy()
    {
        if (!quitting)
        {
            if (isLocalPlayer)
            {
                print("отключилось");
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                //TODO интерфейс отключения
                Application.LoadLevel(0);
                network.Disconnect();
            }
            else 
            {
                RemovePlayer(this);
                players.Remove(this);
            }
        }
    }
    public void EscapeMission() 
    {
        if (isServer) 
        {
            generator.GoBackFailure();
        }
    }
    // Update is called once per frame
    void Update()
    {

        if (isLocalPlayer&&!truestart) { if (GameObject.Find("network").GetComponent<customNetworkHUD>().characterclass != -1) { truestart = true; TrueStart(); } return; }

        someEscapeTrigger = false;
        if (isEscape&&Input.GetKeyDown(KeyCode.Escape))
        {
            isEscape = false;
            UIEscapeMenu.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            someEscapeTrigger = true;
        }
        if (isLocalPlayer&&!missionMenuController.isMissionMenuOpened&&!isEscape)
        {
            FPS = Mathf.Ceil((FPS*9 + (1f / Time.deltaTime))/10);
            if (!isDead)
            {
                if (attackcooldown > 0)
                {
                    attackcooldown -= Time.deltaTime;
                    animator.SetBool("Attack", false);
                }
                if (flarecooldown > 0)
                {
                    flarecooldown -= Time.deltaTime;
                    flarebar.value = 1f-flarecooldown*0.25f;
                }
                if (grenadecooldown > 0&&grenadecount>0)
                {
                    grenadecooldown -= Time.deltaTime;
                    grenadebar.value = 1f-grenadecooldown*0.5f;
                }
                if (containercooldown > 0)
                {
                    containercooldown -= Time.deltaTime;
                }
                isReviving = false;
                mousedelta.x = Input.GetAxis("Mouse X");
                mousedelta.y = Input.GetAxis("Mouse Y");
                transform.Rotate(0, mousedelta.x * Time.deltaTime*2 * (scopeTrg?20f:100f), 0);
                rot += mousedelta.x * Time.deltaTime * 2;
                yrot -= mousedelta.y * Time.deltaTime * 2 * (scopeTrg ? 20f : 100f);
                yrot = Mathf.Clamp(yrot, -90, 90);
                head.transform.localRotation=Quaternion.Euler(yrot,0,0);
                
                timerforcam += Time.deltaTime;
                ismoving = false;
                if(camShake)head.transform.localPosition = new Vector3(Random.Range(-0.125f, 0.125f), 0.5f+Random.Range(-0.125f, 0.125f), 0);
                if (Input.GetKey(KeyCode.W)) {
                    ismoving = true;
                    moveVector = new Vector3(moveVector.x, moveVector.y, acceleration * speed * sprint);
                    head.transform.GetChild(0).localPosition = new Vector3(Mathf.Sin(timerforcam * Mathf.PI * 2) * 0.01f*sprint, Mathf.Cos(timerforcam*Mathf.PI * 4) *0.01f * sprint, 0);

                    //ступеньки
                    if (
                        Physics.Raycast(transform.position + transform.forward * 0.5f + new Vector3(0, 0.1f, 0), -Vector3.up, out hit, 1f)||
                        Physics.Raycast(transform.position + transform.forward * 0.5f + new Vector3(0.05f, 0.1f, 0), -Vector3.up, out hit, 1f)||
                        Physics.Raycast(transform.position + transform.forward * 0.5f + new Vector3(-0.05f, 0.1f, 0), -Vector3.up, out hit, 1f)
                        )
                    {
                        transform.position = transform.position + new Vector3(0, 0.1f, 0);
                    }
                }
                if (Input.GetKey(KeyCode.A))
                {
                    ismoving = true;
                    moveVector = new Vector3(-acceleration * speed * sprint, moveVector.y, moveVector.z);
                    head.transform.GetChild(0).localPosition = new Vector3(Mathf.Sin(timerforcam * Mathf.PI * 2) * 0.01f * sprint, Mathf.Cos(timerforcam * Mathf.PI * 4) * 0.01f * sprint, 0);
                }
                if (Input.GetKey(KeyCode.S))
                {
                    ismoving = true;
                    moveVector = new Vector3(moveVector.x, moveVector.y, -acceleration * speed * sprint);
                    head.transform.GetChild(0).localPosition = new Vector3(Mathf.Sin(timerforcam * Mathf.PI * 2) * 0.01f * sprint, Mathf.Cos(timerforcam * Mathf.PI * 4) * 0.01f * sprint, 0);
                }
                if (Input.GetKey(KeyCode.D))
                {
                    ismoving = true;
                    moveVector = new Vector3(acceleration * speed * sprint, moveVector.y, moveVector.z);
                    head.transform.GetChild(0).localPosition = new Vector3(Mathf.Sin(timerforcam * Mathf.PI*2) * 0.01f * sprint, Mathf.Cos(timerforcam * Mathf.PI * 4) * 0.01f * sprint, 0);
                }
                if (ismoving)
                {
                    acceleration += Time.deltaTime;
                    if (acceleration > 0.1f) { acceleration = 0.1f; }
                }
                else { acceleration = 0; }
                moveVector = transform.TransformDirection(moveVector);
                if (moveVector != new Vector3()) { rb.velocity = (new Vector3(moveVector.x * 1.25f, rb.velocity.y, moveVector.z * 1.25f) + rb.velocity * (2 + (canJump ? 0 : 8))) / (3f + (canJump ? 0 : 8)); }

                if (rb.velocity.y==0&&canJump) { rb.drag = 15f; } else { rb.drag = 0.95f; }
                moveVector = new Vector3();
                if (Input.GetKey(KeyCode.RightControl) && Input.GetKey(KeyCode.F6)) {
                            CmdStartMission();
                }
                if (Input.GetKey(KeyCode.E))
                {
                    if (seeContainer && containercooldown <= 0)
                    {
                        for (int i = 0; i < resourcesCount.Count; ++i)
                        {
                            if (resourcesCount[i] > 0)
                            {
                                CmdAddResource(i, -1);
                                CmdIsDrop(true);
                                generator.AddResource(i, 1);
                                containercooldown = 0.1f;
                                PlayOneShot("event:/высыпание");
                                break;
                            }
                        }
                    }
                    if (gobackinteraction&&isServer)
                    {
                        if (generator.platformstatus == 0)
                        {
                            CmdStartMission();
                        }
                        else if(generator.platformstatus == 2)
                        {
                            CmdEndMission();
                        }
                        gobackinteraction = false;
                    }
                    if (deathInteraction&&targetobject) {
                        CmdAddRevivePoints(targetobject.GetComponent<player>().nickname, Time.deltaTime*50);
                        isReviving = true;
                    }
                    if (missionselectorinteraction) {
                        missionMenuController.GenerateMissions();
                        //generator.ShowMissions();
                    }
                    if (classchangeInteraction) {
                        network.OpenClassSelector();
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                    }
                    if (upgradeWeaponTerminalInteracton)
                    {
                        generator.OpenWeaponUpgrade();
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                    }
                    if (supplyinteracton) 
                    {
                        if (!supplyprogressbar.gameObject.active) 
                        {
                            supplyprogressbar.gameObject.SetActive(true);
                        }
                        supplyprogress += Time.deltaTime;
                        supplyprogressbar.value = supplyprogress/4f;
                        if (supplyprogress >= 4f) { 
                            supplyprogress = 0f;
                            PlayOneShot("event:/высыпание");
                            allammo += weapon.maxammo/2;
                            grenadecount += grenades[characterclass].GetComponent<grenade>().maxcount / 2;
                            if (grenadecount > grenades[characterclass].GetComponent<grenade>().maxcount) { grenadecount = grenades[characterclass].GetComponent<grenade>().maxcount; }
                            CmdDmg(-50);
                            if (allammo >= weapon.maxammo) { allammo = weapon.maxammo; }
                            CmdGetSupply(supply);
                            UIammo.text = ammo + "/" + allammo;
                            UIgrenadecounter.text = grenadecount+"";
                        }
                    }
                }
                else
                {
                    CmdIsDrop(false);
                }

                //инвентарь
                if (Input.GetKeyDown(KeyCode.Q)) {
                    CmdSetSlot(currentslot==0?1:0);
                    scopeTrg = false;
                }
                if (Input.GetKeyDown(KeyCode.Alpha1)) {
                    CmdSetSlot(1);
                }
                if (Input.GetKeyDown(KeyCode.Alpha0)) {
                    CmdSetSlot(0);
                }
                if (Input.GetKeyDown(KeyCode.Alpha4)) 
                {
                    isTryingSupply = !isTryingSupply;
                    supplymarker.SetActive(isTryingSupply);
                    UISupplyCost.SetActive(isTryingSupply);
                }

                //перезарядка
                if (Input.GetKeyDown(KeyCode.R) && reloadtimer <= 0 && allammo > 0)
                {
                    if (allammo > 0)
                    {
                        reloadtimer = weapon.reloadtime;
                        if (allammo >= weapon.ammo)
                        {
                            allammo += ammo - weapon.ammo;
                            ammo = weapon.ammo;
                        }
                        else 
                        {
                            ammo = allammo;
                            allammo = 0;
                        }
                        PlayOneShot("event:/перезарядка", "Parameter 1", weapon.reloadsound);
                    }
                }
                if (reloadtimer > 0) {
                    rotator.transform.localRotation=Quaternion.Euler(45,0,45);
                    reloadtimer -= Time.deltaTime;
                    if (reloadtimer <= 0)
                    {
                        UIammo.text = ammo + "/" + allammo;
                        rotator.transform.localRotation = Quaternion.Euler(0, 0, 0);
                    }
                }

                //прыжки
                if (Input.GetKeyDown(KeyCode.Space)&&canJump) {
                    if (canUpJump) { rb.velocity = new Vector3(rb.velocity.x,0,rb.velocity.z); }
                    rb.AddRelativeForce(0, 450f, 0, ForceMode.Impulse);
                    if (currentslot == 1) { slot1.transform.Rotate(15,0,0); }
                }
                sprint = 1f;
                if (Input.GetKey(KeyCode.LeftShift)){sprint = 1.45f;}
                if (Input.GetKey(KeyCode.Mouse0))
                {
                    if (isTryingSupply)
                    {
                        if (generator.resourcesCount[0] >= 40)
                        {
                            CmdSpawnSupply(supplymarker.transform.position);
                            supplymarker.SetActive(false);
                            UISupplyCost.SetActive(false);
                            isTryingSupply = false;
                            PlayOneShot("event:/click");
                        }
                        else
                        {
                            PlayOneShot("event:/нельзя");
                        }
                    }
                    else
                    {
                        if (attackcooldown <= 0)
                        {
                            if (currentslot == 0)
                            {
                                animator.SetBool("Attack", true);
                                attackcooldown = characterclass != 3 ? 1f : 0.5f;
                                Attack();
                            }
                            else
                            {
                                if (reloadtimer <= 0 && ammo > 0)
                                {
                                    --ammo;
                                    attackcooldown = 1f / weapon.firerate;
                                    UIammo.text = ammo + "/" + allammo;
                                    for (int i = 0; i < weapon.firespershoot; ++i) Fire();
                                }
                            }
                        }
                    
                    }
                }
                if (Input.GetKeyDown(KeyCode.Mouse1)&&slot1.active) 
                {
                    if (weapon.haveScope) { scopeTrg = !scopeTrg; }
                }
                if (Input.GetKeyDown(KeyCode.F))
                {
                    if (flarecooldown <= 0)
                    {
                        flarecooldown = 4f;
                        CmdSpawnFlare();
                    }
                }
                if (Input.GetKeyDown(KeyCode.G))
                {
                    if (grenadecooldown <= 0&&grenadecount>0)
                    {
                        --grenadecount;
                        grenadecooldown = 2f;
                        UIgrenadecounter.text = grenadecount + "";
                        if (grenadecount == 0) {
                        grenadebar.value = 1f-grenadecooldown*0.5f;
                        }
                        CmdSpawnGrenade();
                    }
                }
                if (Input.GetKeyDown(KeyCode.C))
                {
                    Physics.Raycast(head.transform.position, head.transform.forward, out hit, 6);
                    if (hit.transform)
                    {
                        CmdMoveMarker(hit.point);
                    }
                }
                if (Input.GetKeyDown(KeyCode.Escape)&&!someEscapeTrigger)
                {
                    isEscape = true;
                    UIEscapeMenu.SetActive(true);
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }

                //доводка оружия и красивости
                slot1.transform.Rotate(mousedelta.y * Time.deltaTime * 15, -mousedelta.x * Time.deltaTime*15, -mousedelta.y * Time.deltaTime * 15+mousedelta.x*Time.deltaTime*15);
                slot1.transform.rotation = Quaternion.Slerp(slot1.transform.rotation, rotator.transform.rotation,3f*Time.deltaTime);
                slot1.transform.position = Vector3.Slerp(slot1.transform.position, rotator.transform.position, 3f * Time.deltaTime);
                if(characterclass!=-1)if (crosshair.transform.GetChild(characterclass).GetComponent<Image>().color.b < 1f) { crosshair.transform.GetChild(characterclass).GetComponent<Image>().color = crosshair.transform.GetChild(characterclass).GetComponent<Image>().color + new Color(0, Time.deltaTime*3, Time.deltaTime * 3,0); }
                if (rot < -1.8f) { rot += 3.6f; }
                if (rot > 1.8f) { rot -= 3.6f; }
                compass.value = (rot % (3.6f))/(-0.25f*3.6f);//*-(2f/3.65f);
                if (weapon.haveRotor) 
                {
                    slot1.transform.GetChild(characterclass + 1).GetChild(0).rotation = Quaternion.Slerp(slot1.transform.GetChild(characterclass + 1).GetChild(0).rotation, slot1.transform.GetChild(characterclass + 1).GetChild(1).rotation, Time.deltaTime*10f);
                }
                if (slot1.active)
                {
                    sniperscope.SetActive(scopeTrg);
                    if (scopeTrg)
                    {
                        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 10, Time.deltaTime * 6);
                    }
                    else
                    {
                        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 60, Time.deltaTime * 6);
                    }
                }

                //UI
                if (uiweapontimer > 0) { uiweapontimer -= Time.deltaTime; } else if(uiweapontimer>-3){ uiweapontimer = -3;UIweapon.SetActive(false); }
                GameObject[] objs = GameObject.FindGameObjectsWithTag("UI");
                for (int i = 0; i < objs.Length; ++i) 
                {
                    objs[i].transform.LookAt(head.transform.position);
                }/**/

                //удаление
                
                for (int i = 0; i < damageinfos.Count; ++i) 
                {
                    if (damageinfos[i].lifetime+3f < Time.realtimeSinceStartup) { damageinfos.Remove(damageinfos[i]); }
                }/**/

                //взаимодействие
                UIE.SetActive(seeInteraction);
                seeInteraction = false;
                missionselectorinteraction = false;
                gobackinteraction = false;
                deathInteraction = false;
                seeContainer = false;
                supplyinteracton = false;
                classchangeInteraction = false;
                upgradeWeaponTerminalInteracton = false;
                Physics.Raycast(head.transform.position, head.transform.forward, out hit, 6);
                if (hit.transform)
                {
                    if (hit.collider.transform.CompareTag("Container"))
                    {
                        seeContainer = true;
                        seeInteraction = true;
                    }
                    if (hit.collider.transform.name == "redbutton")
                    {
                        seeInteraction = true;
                        gobackinteraction = true;
                    }
                    if (hit.collider.transform.name == "missionselector")
                    {
                        seeInteraction = true;
                        missionselectorinteraction = true;
                    }
                    if (hit.collider.transform.name == "Supply(Clone)"&& hit.collider.GetComponent<supply>().amount>0)
                    {
                        seeInteraction = true;
                        supplyinteracton = true;
                        supply = hit.collider.GetComponent<supply>().netId;
                    }
                    if (hit.collider.transform.name == "ClassChanger")
                    {
                        seeInteraction = true;
                        classchangeInteraction = true;
                    }
                    if (hit.collider.transform.name == "WeaponTerminal")
                    {
                        seeInteraction = true;
                        upgradeWeaponTerminalInteracton = true;
                    }
                    if (hit.collider.transform.CompareTag("Player")&&hit.collider.transform.GetComponent<player>().hp<=0)
                    {
                        seeInteraction = true;
                        deathInteraction = true;
                        targetobject = hit.collider.transform.gameObject;
                    }
                }
                if (!supplyinteracton) { supplyprogress = 0f;supplyprogressbar.gameObject.SetActive(false); }
                
                if (Physics.Raycast(transform.position, -transform.up, out hit, 6))
                {
                //    transform.parent = hit.transform;
                }

                //размеры помещения(для звука)
                float a = 0;
                if (Physics.Raycast(transform.position, -transform.up, out hit, 30)) { a += hit.distance; } else { a += 30; }
                if (Physics.Raycast(transform.position, transform.up, out hit, 30)) { a += hit.distance; } else { a += 30; }
                if (Physics.Raycast(transform.position, -transform.forward, out hit, 30)) { a += hit.distance; } else { a += 30; }
                if (Physics.Raycast(transform.position, transform.forward, out hit, 30)) { a += hit.distance; } else { a += 30; }
                if (Physics.Raycast(transform.position, -transform.right, out hit, 30)) { a += hit.distance; } else { a += 30; }
                if (Physics.Raycast(transform.position, transform.right, out hit, 30)) { a += hit.distance; } else { a += 30; }
                a /= 2;
                roomsize = (roomsize + a) / 2;
                FMODUnity.RuntimeManager.StudioSystem.setParameterByName("roomsize",roomsize);

                //  if (GameObject.Find("SphereDestroyer(Clone)")) { marchingspace.isChecking = true;  } else { marchingspace.isChecking = false; }
                magnitude = rb.velocity.y;//rb.velocity.magnitude;
                if (hp < 0)
                {
                    PlayOneShot("event:/ouch", "ID", 0);
                    isDead = true;
                    firstcamera.SetActive(false);
                    diecamera.SetActive(true);
                    CmdDie();
                }

                //щиты
                shldbar.value = shield * 0.01f;
                UIshld.text = shield.ToString();
                if (shield < 100)
                {
                    if (shieldcooldown > 0) { shieldcooldown -= Time.deltaTime; } else { ++shield; shieldcooldown = 0.1f; }
                }

                //инвентарь
                UpdateResources();

                //обновление класса при смене
                if (network.characterclass != characterclass) {
                    rank = network.dwarfprestige[network.characterclass] > 10 ? 5 : (network.dwarfprestige[network.characterclass] > 0 ? 4 : (network.dwarfeliteranks[network.characterclass] > 0 ? network.dwarfeliteranks[network.characterclass] : 0));
                    CmdSetClass(network.characterclass,rank,network.characterclass);
                    classpic.sprite = classes[network.characterclass].icon;
                    classpic.transform.GetChild(0).GetComponent<Text>().text = network.dwarflevels[network.characterclass].ToString();
                    classpic.transform.GetChild(1).GetComponent<Image>().sprite = rankpics[rank];
                }

                //возрождение другого
                if (isReviving)
                {
                    UIRevive.SetActive(true);
                    UIRevive.GetComponent<Slider>().value = generator.GetRevivePoints(targetobject.GetComponent<player>().nickname)*0.01f;
                }
                else {
                    UIRevive.SetActive(false);
                }

                //ui игроки
                if (updatetimer == 0) {
                    for (int i = 0; i < players.Count; ++i) {
                        UpdatePlayer(players[i]);
                    }
                    if (characterclass==4&&GameObject.FindGameObjectWithTag("Hive"))
                    {
                        UIscaner.SetActive(true);
                        float min = 999f;
                        for (int i = 0; i < GameObject.FindGameObjectsWithTag("Hive").Length; ++i)
                        {
                            if (Vector3.Distance(transform.position, GameObject.FindGameObjectsWithTag("Hive")[i].transform.position) < min)
                            {
                                min = Vector3.Distance(transform.position, GameObject.FindGameObjectsWithTag("Hive")[i].transform.position);
                            }
                        }
                        UIscaner.GetComponent<Animator>().speed = 1f+(20/min);
                    }
                    else
                    {
                        UIscaner.SetActive(false);
                    }
                    updatetimer = 300;
                }
                else { --updatetimer; }

            }
            else {
                mousedelta.x = Input.GetAxis("Mouse X");
                mousedelta.y = Input.GetAxis("Mouse Y");
                diecamera.transform.Rotate(0, mousedelta.x * Time.deltaTime * 100f, 0);
                diecamera.transform.GetChild(0).transform.Rotate(-mousedelta.y * Time.deltaTime * 100f, 0, 0);

                if (prevrevivepoints != generator.GetRevivePoints(nickname))
                {
                    UIRevive.SetActive(true);
                    UIRevive.GetComponent<Slider>().value = generator.GetRevivePoints(nickname) * 0.01f;
                }
                else { 
                    UIRevive.SetActive(false);
                }

                prevrevivepoints = generator.GetRevivePoints(nickname)*0.01f;
                if (hp == 100)
                {
                    isDead = false;
                    firstcamera.SetActive(true);
                    diecamera.SetActive(false);
                }
                else {
                    CmdTryRevive();
                }
            }
            if (shieldpic.color.a > 0) { shieldpic.color = new Color(1, 1, 1, shieldpic.color.a-0.15f*Time.deltaTime); }
            if (shieldpic.color.a < 0) { shieldpic.color = new Color(1, 1, 1, 0); }
        }
        if (isServer)
        {
            GameObject[] bugs = GameObject.FindGameObjectsWithTag("Bug");
            for (int i = 0; i < damageinfos.Count; ++i)
            {
                if (damageinfos[i].lifetime + 1f < Time.realtimeSinceStartup) { damageinfos.Remove(damageinfos[i]);continue; }
                for (int ii = 0; ii < bugs.Length; ++ii) {
                    if (bugs[ii].GetComponent<NetworkIdentity>().netId == damageinfos[i].netid&& bugs[ii].GetComponent<bug>().hp<=0) {
                        ++kills;
                        damageinfos.Remove(damageinfos[i]);
                        break;
                    }
                }
            }
        }

        if (localhp != hp) {
            if(hp!=99&&hp<=localhp)PlayOneShot("event:/ouch", "ID", 1);
            localhp = hp;
            if (!isLocalPlayer) { hpobject.text = nickname + "\n" + hp; } else { UIhp.text = hp.ToString(); hpbar.value = hp*0.01f; }
        }
        canJump = false;
        canUpJump = false;
        
        if (
            Physics.Raycast(transform.position, -transform.up, out hit, 1.25f)||
            Physics.Raycast(transform.position+transform.forward/2, -transform.up, out hit, 1.25f)||
            Physics.Raycast(transform.position - transform.forward / 2, -transform.up, out hit, 1.25f)||
            Physics.Raycast(transform.position + transform.right / 2, -transform.up, out hit, 1.25f)||
            Physics.Raycast(transform.position - transform.right / 2, -transform.up, out hit, 1.25f)
            ) {
            if (isLocalPlayer) { canJump = true; }
            if (isServer && hit.collider.transform.CompareTag("Chunk")) {
                thissmell.transform.position = transform.position;//hit.point;
            }
        }
        if (Physics.Raycast(head.transform.position+transform.forward*2+new Vector3(0,0.1f,0), -Vector3.up, out hit, 1f)) {
            Debug.DrawRay(hit.point, Vector3.up, Color.cyan);
            if (isLocalPlayer) { canJump = true;canUpJump = true; }
        }
        
        if (generator.platformstatus==1||generator.platformstatus==3) { 
            if (isLocalPlayer&&!isClear) { 
                Clear();
                if (generator.platformstatus == 1) { CmdResetKills(); } 
            }
            if (generator.platformstatus == 1)
            {
                if (!isFade&&isLocalPlayer)
                {
                    UIBlackScreen.color = UIBlackScreen.color - new Color(0, 0, 0, Time.deltaTime * 0.125f);
                    if (UIBlackScreen.color.a <= 0) { UIBlackScreen.gameObject.SetActive(false); isFade = true; UIBlackScreen.color = new Color(0, 0, 0, 1); }
                }
            }
            else { if (isFade) { isFade = false; } }
            if(!generator.isFailure)transform.parent = generator.platform.transform;
            if (!isTeleported) {
                transform.position = !generator.isFailure ? generator.platform.transform.GetChild(Random.Range(0, 8)).position : generator.respawnpoint.transform.position;
                isTeleported = true;
            } 
        }
        if (generator.platformstatus==0||generator.platformstatus==2) {
            transform.parent = null;isTeleported = false;isClear = false;
            if (generator.isStart&&isLocalPlayer) { 
                CmdSetProgress(generator.GetLoadingStatus()); 
            }
            fog.enabled = generator.platformstatus != 0;
        }
        allitems = GameObject.FindGameObjectsWithTag("Item");
        int id;
        for (int i = 0; i < allitems.Length; ++i) {
            id = int.Parse(allitems[i].name);
            if (resourcesCount[id] < resources[id].maxInBag)
            {
                if (Vector3.Distance(allitems[i].transform.position, transform.position) < 3f)
                {
                    allitems[i].transform.position -= (allitems[i].transform.position - transform.position) * Time.deltaTime * 4;
                }
            }
        }

        //красивость
        if (Vector3.Distance(oldpos, transform.position)>2.4f&&canJump&&(generator.platformstatus==0||generator.platformstatus==2))
        {
            oldpos = transform.position;
            PlayOneShot("event:/step");
        }

        //тики для тикового урона(логично)
        tickf += Time.deltaTime;
    }
    private void PlayOneShot(string eventname) 
    {
        FMOD.Studio.EventInstance instance = FMODUnity.RuntimeManager.CreateInstance(eventname);
        instance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(gameObject));
        FMODUnity.RuntimeManager.AttachInstanceToGameObject(instance, transform, rb);
        instance.start();
        instance.release();
    }
    private void PlayOneShot(string eventname,string paramname, int paramvalue) 
    {
        FMOD.Studio.EventInstance instance = FMODUnity.RuntimeManager.CreateInstance(eventname);
        instance.setParameterByName(paramname, paramvalue);
        instance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(gameObject));
        FMODUnity.RuntimeManager.AttachInstanceToGameObject(instance, transform, rb);
        instance.start();
        instance.release();
    }
   /*private void OnGUI()
    {
     //   GUI.Box(new Rect(Screen.width - 200, Screen.height - 20, 200, 20), FPS + " FPS");
        GUI.Box(new Rect(Screen.width - 200, Screen.height - 20, 200, 20), roomsize + " roomsize");
    } */
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Item"))
        {
            int id = int.Parse(collision.gameObject.name);
            if(resourcesCount[id]<resources[id].maxInBag){
                if (isLocalPlayer)
                {
                    CmdAddResource(id,1);
                }
                PlayOneShot("event:/кристалл_подобран");
                Destroy(collision.gameObject);
            }
        }
        if (isLocalPlayer)
        {
          //  print(magnitude);
            if (magnitude < -5) {//7
                Dmg((int)(-(magnitude+5)*8));
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Damager"))
        {
            if (isLocalPlayer)
            {
                int dmg = 15;
                //int dmg = int.Parse(other.gameObject.name);
                Dmg(dmg);
            }
          //  Destroy(other.gameObject);
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("Damager")&&tickf>1)
        {
            print(tickf);
            tickf = 0;
            if (isLocalPlayer)
            {
                Dmg(15);
            }
        }
    }
    public static readonly int[] levels = {
    3000,
    4000,
    5000,
    6000,
    7000,
    8000,
    9000,
    10000,
    11000,
    12000,
    13000,
    14000,
    15000,
    15500,
    16000,
    16500,
    17000,
    17500,
    18000,
    18500,
    19000,
    19500,
    20000,
    20500
    };
}
