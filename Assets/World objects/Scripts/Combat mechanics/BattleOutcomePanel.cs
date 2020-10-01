using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleOutcomePanel : MonoBehaviour
{
    public void SetOutcome(IEnumerable<(Ship, string)> faction1Ships, IEnumerable<(Ship, string)> faction2Ships)
    {
        bool outcome1 = SetOutcomeSingle(Faction1ResultsBox, faction1Ships);
        bool outcome2 = SetOutcomeSingle(Faction2ResultsBox, faction2Ships);
        if (outcome1 && !outcome2)
        {
            OutcomeTextBox.text = "Victory!";
        }
        else if (!outcome1 && outcome2)
        {
            OutcomeTextBox.text = "Defeat!";
        }
        else
        {
            OutcomeTextBox.text = "Inconclusive battle";
        }
    }

    private bool SetOutcomeSingle(RectTransform resultsBox, IEnumerable<(Ship, string)> ships)
    {
        StackingLayout stackingBehavior = resultsBox.GetComponent<StackingLayout>();
        stackingBehavior.AutoRefresh = false;

        float totalHeight = 0f;
        bool hasEnabledShips = false;
        foreach ((Ship, string) item in ships)
        {
            RectTransform t = Instantiate(ResultsItemTemplate);

            t.SetParent(resultsBox, false);
            float height = t.rect.height;
            totalHeight += height;
            float pivotOffset = t.pivot.x * t.rect.width;
            t.anchoredPosition = new Vector2(pivotOffset, 0);

            TextMeshProUGUI textElem = t.GetComponentInChildren<TextMeshProUGUI>();
            string status = string.Empty;
            if (item.Item1.ShipDisabled && item.Item1.HullHitPoints == 0)
            {
                status = "Destroyed";
                textElem.color = new Color(200f / 255f, 0f, 0f);
            }
            else if (item.Item1.ShipDisabled)
            {
                status = "Disabled";
                textElem.color = new Color(255f / 255f, 85f / 255f, 0f);
            }
            else
            {
                hasEnabledShips = true;
            }

            textElem.text = string.Format("{0}\n{1}", item.Item1.DisplayName.ShortName, status);

            Button buttonElem = t.GetComponentInChildren<Button>();
            Destroy(buttonElem.gameObject);

            Image img = t.Find("Image").GetComponent<Image>();
            Sprite shipSprite = null;
            ObjectFactory.GetGenericPhoto(item.Item2, out shipSprite);
            img.sprite = shipSprite;
        }

        resultsBox.sizeDelta = new Vector2(resultsBox.sizeDelta.x, totalHeight);

        stackingBehavior.ForceRefresh();
        stackingBehavior.AutoRefresh = true;
        return hasEnabledShips;
    }

    public void Finish()
    {
        ObjectFactory.ClearBattleSceneCache();
        SceneStack.ReturnFromScene<BattleSceneParamsReader.BattleSceneParams>();
    }

    public TextMeshProUGUI OutcomeTextBox;
    public RectTransform Faction1ResultsBox;
    public RectTransform Faction2ResultsBox;
    public RectTransform ResultsItemTemplate;
}
