using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MissionMenuController : MonoBehaviour
{
    public Generator generator;
    public difficulty[] difficulties;
    public mutator[] mutators;
    public mission[] missiontypes;
    public supplylevel[] supplylevels;
    public Gradient multyplexergradient;
    public int multiplexvalue = 0;
    public int difficultyvalue=0,supplyvalue=0,missiontypevalue=0;
    public GameObject UI, bottomPanel, questbase, questprefab;
    public Text diffmult, diffname, supmult, supname, mismult, misname, misdes, allmult;
    public Image difpic, suppic, mispic;
    private List<quest> missions;
    private Generator.questtype currentquest;
    private int questtarget, questparam, questdifficulty;
    private int selectedquest = -1;
    private string funnyname;
    public bool isMissionMenuOpened;
    private void Start()
    {
        missions = new List<quest>();
    }
    public void CalculateMultiplex() {
        multiplexvalue = (supplylevels[supplyvalue].bonus + difficulties[difficultyvalue].bonus+missiontypes[missiontypevalue].bonus);
        allmult.text =  multiplexvalue+"%";
        allmult.color = multyplexergradient.Evaluate(multiplexvalue<1000?(multiplexvalue*0.001f):(1f));
    }
    public void StepSupply(int d)
    {
        return;//TODO ВЕРНУТЬ
        supplyvalue += d;
        if (supplyvalue < 0) supplyvalue = 0;
        if (supplyvalue > supplylevels.Length-1) supplyvalue = supplylevels.Length-1;
        supmult.text = "+" + supplylevels[supplyvalue].bonus+"%";
        supname.text = supplylevels[supplyvalue].supplyname;
        suppic.sprite = supplylevels[supplyvalue].icon;
        CalculateMultiplex();
    }
    public void StepDifficulty(int d)
    {
        difficultyvalue += d;
        if (difficultyvalue < 0) difficultyvalue = 0;
        if (Input.GetKey(KeyCode.LeftAlt))
        {
            //TODO ВЕРНУТЬ
            if (difficultyvalue > difficulties.Length - 1) difficultyvalue = difficulties.Length - 1;
        }
        else
        {
            if (difficultyvalue > 4) difficultyvalue = 4;
        }
        diffmult.text = "+" + difficulties[difficultyvalue].bonus+"%";
        diffname.text = difficulties[difficultyvalue].difname;
        difpic.sprite = difficulties[difficultyvalue].icon;
        CalculateMultiplex();
    }
    public void GenerateMissions() {
        List<int> seedlist = new List<int>();
        missions = new List<quest>();
        for (int i = 0; i < 5; ++i)
        {
            seedlist.Add(Random.Range(int.MinValue, int.MaxValue));
        }
        for (int i = 0; i < 5; ++i)
        {
            Random.seed = seedlist[i];
            funnyname = Generator.funnyA[Random.Range(0, Generator.funnyA.Length)] + " " + Generator.funnyB[Random.Range(0, Generator.funnyB.Length)];
            currentquest = (Generator.questtype)Random.Range(0, 5);
            if (currentquest == Generator.questtype.Добыча)
            {
                questtarget = Random.Range(0, 4);
                questparam = Random.Range(2, 3) * generator.resources[questtarget].maxInBag;//7,10
            }
            else if (currentquest == Generator.questtype.Поиск)
            {
                questtarget = 4;
                questparam = Random.Range(2, 5) * 5;
            }
            else if (currentquest == Generator.questtype.Подавление)
            {
                questtarget = 0;
                questparam = Random.Range(2, 4);
            }
            else if (currentquest == Generator.questtype.Ликвидация)
            {
                questtarget = 0;
                questparam = Random.Range(2, 4);
            }
            else if (currentquest == Generator.questtype.Бойня)
            {
                questtarget = 0;
                questparam = Random.Range(2, 6)*20;
            }
            missions.Add(new quest(funnyname, currentquest, questtarget, questparam, seedlist[i]));
        }
        isMissionMenuOpened = true;
        UI.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SpawnQuests();
    }
    public void SpawnQuests() {
        for (int i = 0; i < questbase.transform.childCount; ++i) { Destroy(questbase.transform.GetChild(i).gameObject); }
        for (int i = 0; i < missions.Count; ++i) {
            GameObject ob = Instantiate(questprefab, questbase.transform.position, questbase.transform.rotation, questbase.transform);
            ob.transform.localPosition = Generator.FibSphere(i, missions.Count, 250, false);
            ob.GetComponent<missionActivator>().id = i;
            ob.transform.Find("Mission Multiplexer").GetComponent<Text>().text = "+"+missiontypes[(int)missions[i].questtype].bonus;
            ob.transform.Find("Mission pic").GetComponent<Image>().sprite = missiontypes[(int)missions[i].questtype].icon;
            ob.transform.Find("Mission Name").GetComponent<Text>().text = missions[i].name;
            if (missions[i].questtype == Generator.questtype.Добыча) { ob.transform.Find("Mission Description").GetComponent<Text>().text = "Добудьте "+missions[i].questparam+" "+generator.resources[missions[i].questtarget].name; }
            if (missions[i].questtype == Generator.questtype.Поиск) { ob.transform.Find("Mission Description").GetComponent<Text>().text = "Добудьте "+missions[i].questparam+" "+generator.resources[missions[i].questtarget].name; }
            if (missions[i].questtype == Generator.questtype.Подавление) { ob.transform.Find("Mission Description").GetComponent<Text>().text = "Уничтожьте "+missions[i].questparam+" улья"; }
            if (missions[i].questtype == Generator.questtype.Ликвидация) { ob.transform.Find("Mission Description").GetComponent<Text>().text = "Нейтрализуйте "+missions[i].questparam+" дисцидалов"; }
            if (missions[i].questtype == Generator.questtype.Бойня) { ob.transform.Find("Mission Description").GetComponent<Text>().text = "Ликвидируйте "+missions[i].questparam+" жуков"; }

        }
    }
    public void SelectMission(int id) {
        selectedquest = id;
        bottomPanel.SetActive(true);
        missiontypevalue = (int)missions[id].questtype;
        mispic.sprite = missiontypes[missiontypevalue].icon;
        misname.text = missions[id].name;
            if (missions[id].questtype == Generator.questtype.Добыча) { misdes.text = "Добудьте " + missions[id].questparam + " " + generator.resources[missions[id].questtarget].name; }
            if (missions[id].questtype == Generator.questtype.Поиск) { misdes.text = "Найдите и добудьте " + missions[id].questparam + " " + generator.resources[missions[id].questtarget].name; }
            if (missions[id].questtype == Generator.questtype.Подавление) { misdes.text = "Уничтожьте " + missions[id].questparam + " улья"; }
            if (missions[id].questtype == Generator.questtype.Ликвидация) { misdes.text = "Нейтрализуйте " + missions[id].questparam + " дисцидалов"; }
            if (missions[id].questtype == Generator.questtype.Бойня) { misdes.text = "Ликвидируйте " + missions[id].questparam + " жуков"; }
        
        mismult.text = "+" + missiontypes[missiontypevalue].bonus;
    }
    public void ApplyMission() {
        generator.seed = missions[selectedquest].seed;
        generator.SetDifficulty(difficultyvalue);
        CloseWindow();
    }
    public void CloseWindow()
    {
        isMissionMenuOpened=false;
        bottomPanel.SetActive(false);
        UI.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    public class quest
    {
        public string name;
        public Generator.questtype questtype;
        public int questtarget;
        public int questparam;
        public int seed;
        public quest(string name, Generator.questtype questtype, int questtarget, int questparam, int seed)
        {
            this.name = name;
            this.questtype = questtype;
            this.questtarget = questtarget;
            this.questparam = questparam;
            this.seed = seed;
        }
    }
    private void Update()
    {
        if (Input.GetKey(KeyCode.Escape)&&isMissionMenuOpened) { CloseWindow(); }
    }
}
