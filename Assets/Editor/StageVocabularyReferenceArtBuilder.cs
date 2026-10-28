using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Builds a layered visual skin over the EXISTING review controls.
/// Runs ONLY on the isolated Stage Select review-art scene. Does not alter
/// progression, scene routing, review profile, or controller event references.</summary>
public static class StageVocabularyReferenceArtBuilder
{
    private const string IsolatedScene =
        "Assets/Scenes/StageSelectBook_VocabularyReviewArt_Isolated.unity";
    private const string SpritesPath = "Assets/Sprites/VocabularyReviewV1/";
    private static readonly Color Navy = new Color(.038f,.081f,.135f,1f);
    private static readonly Color Gold = new Color(.96f,.80f,.55f,1f);
    private static readonly Color ParchmentInk = new Color(.11f,.075f,.060f,1f);
    private static readonly Color Ivory = new Color(.96f,.94f,.90f,1f);

    [MenuItem("SPELLSTRIKE/UI/Vocabulary Review/Build Isolated Reference Art")]
    public static void Build()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (Application.isPlaying || EditorApplication.isCompiling ||
            scene.path != IsolatedScene || scene.isDirty)
            throw new InvalidOperationException(
                "Open and save the isolated VocabularyReviewArt scene in Edit Mode first.");

        ImportSprites();
        GameObject canvas = null;
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == "VocabularyReviewCanvas") canvas = root;
        if (canvas == null) throw new InvalidOperationException("Original review Canvas not found.");
        var ctrl = canvas.GetComponent<StageVocabularyReviewController>();
        if (ctrl == null || canvas.GetComponents<StageVocabularyReviewController>().Length != 1)
            throw new InvalidOperationException("Exactly one existing review controller required.");

        var reviewRoot=canvas.transform.Find("ReviewRoot") as RectTransform;
        if (reviewRoot==null) throw new InvalidOperationException("ReviewRoot not found.");
        if (reviewRoot.Find("ReferenceContentRoot") != null)
            throw new InvalidOperationException(
                "ReferenceContentRoot already exists. Refusing to rebuild/move live UI.");

        var dim=Required(reviewRoot,"DimOverlay").GetComponent<Image>();
        dim.rectTransform.anchorMin=Vector2.zero;
        dim.rectTransform.anchorMax=Vector2.one;
        dim.rectTransform.offsetMin=Vector2.zero;
        dim.rectTransform.offsetMax=Vector2.zero;
        dim.color=Color.black;
        dim.raycastTarget=true;
        dim.transform.SetAsFirstSibling();

        var content=NewRect(reviewRoot,"ReferenceContentRoot");
        content.anchorMin=content.anchorMax=new Vector2(.5f,.5f);
        content.pivot=new Vector2(.5f,.5f);
        content.anchoredPosition=Vector2.zero;
        content.sizeDelta=new Vector2(1672,941);
        content.gameObject.AddComponent<StageVocabularyReferenceFit>();
        content.SetAsLastSibling();

        // Reparent the already serialized objects. Their instance IDs and
        // existing StageVocabularyReviewController references remain unchanged.
        var panel=Move(reviewRoot,content,"ReviewPanel");
        var header=Move(reviewRoot,content,"Header");
        var question=Move(reviewRoot,content,"QuestionText");
        var answers=Move(reviewRoot,content,"AnswerButtons");
        var feedback=Move(reviewRoot,content,"FeedbackGroup");
        var summary=Move(reviewRoot,content,"SummaryPanel");
        var progress=Move(reviewRoot,content,"ProgressIndicators");
        var continueRoot=Move(reviewRoot,content,"ContinueButton");

        var panelImage=panel.GetComponent<Image>();
        panelImage.sprite=Sprite("vocab_review_panel");
        panelImage.color=Color.white;
        panelImage.type=Image.Type.Sliced;
        panelImage.raycastTarget=false;
        Place(panel,119,59,1434,807);
        panel.SetAsFirstSibling();

        var divider=ImageAt(content,"DecorativeHeaderDivider",
            Sprite("vocab_header_divider"),316,147,1040,27);
        divider.raycastTarget=false;
        var wordUnderline=ImageAt(content,"WordUnderline",
            Sprite("vocab_word_underline"),659,232,354,30);
        wordUnderline.raycastTarget=false;
        divider.transform.SetSiblingIndex(1);
        wordUnderline.transform.SetSiblingIndex(2);

        Fill(header);
        SetText(Required(header,"TitleText"),420,85,825,60,44,Gold,
            true,42,48);
        SetText(Required(header,"TargetWordText"),390,174,892,78,69,Gold,
            true,52,76);
        SetText(Required(header,"ProgressText"),1350,85,105,52,30,Gold,
            true,25,34);
        SetText(question,313,251,1048,52,28,Ivory,true,23,31);

        Fill(answers);
        for(int i=0;i<4;i++)
        {
            var answer=Required(answers,"AnswerButton_"+(i+1));
            Place(answer,263,313+i*106,1148,90);
            var plaque=answer.GetComponent<Image>();
            var button=answer.GetComponent<Button>();
            if(plaque==null||button==null)
                throw new InvalidOperationException("Original answer button components missing.");
            plaque.sprite=Sprite("vocab_answer_plaque");
            plaque.color=Color.white;
            plaque.type=Image.Type.Sliced;
            plaque.raycastTarget=true;
            button.targetGraphic=plaque;

            var badge=Required(answer,"LetterBadge");
            Place(badge,-9,0,122,90);
            var badgeImage=badge.GetComponent<Image>();
            badgeImage.sprite=Sprite("vocab_answer_letter_badge");
            badgeImage.type=Image.Type.Sliced;
            badgeImage.color=Color.white;
            badgeImage.raycastTarget=false;
            SetText(Required(badge,"LetterText"),28,17,60,57,47,Gold,true,40,49);
            SetText(Required(answer,"AnswerText"),140,10,978,70,30,ParchmentInk,
                false,19,30);
        }

        // Feedback replaces the word/question header only, leaving the original
        // four answer plaques visible for their correct/incorrect sprite states.
        Fill(feedback);
        var feedbackBackground=Required(feedback,"Image");
        Place(feedbackBackground,263,106,1148,199);
        var fbImage=feedbackBackground.GetComponent<Image>();
        fbImage.sprite=Sprite("vocab_feedback_panel");
        fbImage.color=Color.white;
        fbImage.type=Image.Type.Sliced;
        fbImage.raycastTarget=false;
        fbImage.transform.SetAsFirstSibling();
        var accent=ImageAt(feedback,"FeedbackAccent",Sprite("vocab_feedback_correct_accent"),
            263,106,1148,199);
        accent.type=Image.Type.Sliced;
        accent.raycastTarget=false;
        accent.gameObject.SetActive(false);
        accent.transform.SetSiblingIndex(1);
        SetText(Required(feedback,"FeedbackTitleText"),310,116,1050,48,35,Gold,true,28,38);
        SetText(Required(feedback,"FeedbackExplanationText"),308,170,1054,123,25,Ivory,
            false,19,25);

        var cont=continueRoot.GetComponent<Image>();
        var contButton=continueRoot.GetComponent<Button>();
        if(cont==null||contButton==null)throw new InvalidOperationException("Continue components missing.");
        Place(continueRoot,1158,760,342,73);
        cont.sprite=Sprite("vocab_continue_disabled");
        cont.type=Image.Type.Sliced;
        cont.color=Color.white;
        contButton.targetGraphic=cont;
        SetText(Required(continueRoot,"ContinueButtonText"),26,5,290,62,38,Gold,true,30,40);
        Required(continueRoot,"ContinueButtonText").GetComponent<TMP_Text>().text="CONTINUE";
        continueRoot.SetAsLastSibling();

        Fill(progress);
        // The existing controller repositions this row for variable-length
        // reviews and clones Diamond_4 as needed. Keep that hierarchy intact.
        // Keep the row's *center* at (835,790); Diamond children are
        // center-anchored and the controller adjusts their X for 4–7 items.
        Place(progress,610,760,450,60);
        for(int i=0;i<4;i++)
        {
            var diamond=Required(progress,"Diamond_"+(i+1));
            var rt=diamond as RectTransform;
            rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);
            rt.pivot=new Vector2(.5f,.5f);
            rt.sizeDelta=new Vector2(48,48);
            rt.anchoredPosition=new Vector2((i-1.5f)*73f,0);
            rt.localScale=Vector3.one;
            rt.localRotation=Quaternion.identity;
            var baseImage=diamond.GetComponent<Image>();
            baseImage.sprite=Sprite("vocab_progress_pending");
            baseImage.type=Image.Type.Simple;
            baseImage.color=Color.white;
            baseImage.preserveAspect=true;
            baseImage.raycastTarget=false;
            var fill=Required(diamond,"ProgressFill");
            Fill(fill);
            fill.GetComponent<Image>().sprite=Sprite("vocab_progress_current");
            fill.GetComponent<Image>().type=Image.Type.Simple;
            fill.GetComponent<Image>().color=Color.white;
            fill.GetComponent<Image>().preserveAspect=true;
            fill.GetComponent<Image>().raycastTarget=false;
            fill.GetComponent<Image>().enabled=false;
            var oldOutline=diamond.Find("EmptyDiamond");
            if(oldOutline!=null)oldOutline.gameObject.SetActive(false);
        }

        // Existing summary labels/values stay live. Only their boxes/art
        // change; the review controller still computes all strings and scores.
        Place(summary,282,165,1108,617);
        var summaryArt=summary.GetComponent<Image>();
        if(summaryArt==null)summaryArt=summary.gameObject.AddComponent<Image>();
        summaryArt.sprite=Sprite("vocab_summary_panel");
        summaryArt.type=Image.Type.Sliced;
        summaryArt.raycastTarget=false;
        summaryArt.color=Color.white;
        SetText(Required(summary,"SummaryTitleText"),93,43,925,77,39,Gold,true,31,43);
        var score=Required(summary,"ScoreImage");
        Place(score,70,158,355,310);
        var scoreImg=score.GetComponent<Image>();
        scoreImg.sprite=Sprite("vocab_summary_score_frame");
        scoreImg.color=Color.white;
        scoreImg.type=Image.Type.Sliced;
        scoreImg.raycastTarget=false;
        SetText(Required(score,"ScoreLabelText"),35,67,285,65,32,Gold,true,23,34);
        SetText(Required(score,"ScoreValueText"),46,145,265,93,63,Ivory,true,45,71);
        var needs=Required(summary,"ForReviewImage");
        Place(needs,454,182,572,269);
        var needsImg=needs.GetComponent<Image>();
        needsImg.sprite=Sprite("vocab_summary_needs_review_frame");
        needsImg.type=Image.Type.Sliced;
        needsImg.color=Color.white;
        needsImg.raycastTarget=false;
        SetText(Required(needs,"NeedsReviewLabelText"),33,35,496,59,31,Gold,true,22,33);
        SetText(Required(needs,"NeedsReviewText"),42,95,488,139,27,Ivory,false,19,29);
        SetText(Required(summary,"CompletionText"),109,504,890,70,25,Ivory,true,20,29);
        summary.gameObject.SetActive(false);

        var skin=canvas.GetComponent<StageVocabularyReviewArtSkin>();
        if(skin==null)skin=canvas.AddComponent<StageVocabularyReviewArtSkin>();
        skin.Configure(
            Sprite("vocab_answer_plaque"),Sprite("vocab_answer_hover"),
            Sprite("vocab_answer_selected"),Sprite("vocab_answer_correct"),
            Sprite("vocab_answer_incorrect"),Sprite("vocab_progress_pending"),
            Sprite("vocab_progress_current"),Sprite("vocab_progress_completed"),
            Sprite("vocab_continue_button"),Sprite("vocab_continue_hover"),
            Sprite("vocab_continue_disabled"),accent,
            Sprite("vocab_feedback_correct_accent"),Sprite("vocab_feedback_incorrect_accent"));

        // Readability defaults only; all words, answers and scores remain
        // under the original controller/profile. Preview canvas stays inactive
        // until the mandatory review is actually triggered.
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[VOCAB REFERENCE ART] Isolated scene assembled with live TMP and original review controller.");
    }

    private static void ImportSprites()
    {
        foreach(string f in Directory.GetFiles(SpritesPath,"*.png"))
        {
            string path=f.Replace('\\','/');
            var t=AssetImporter.GetAtPath(path) as TextureImporter;
            if(t==null)
            {
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
                t=AssetImporter.GetAtPath(path) as TextureImporter;
            }
            if(t==null)throw new InvalidOperationException("Missing TextureImporter: "+path);
            t.textureType=TextureImporterType.Sprite;
            t.spriteImportMode=SpriteImportMode.Single;
            t.spritePixelsPerUnit=400f;
            t.alphaIsTransparency=true;
            t.mipmapEnabled=false;
            t.wrapMode=TextureWrapMode.Clamp;
            t.filterMode=FilterMode.Bilinear;
            t.maxTextureSize=4096;
            if(path.Contains("answer_")||path.Contains("continue_")||
                path.Contains("review_panel")||path.Contains("feedback_")||
                path.Contains("summary_"))
                t.spriteBorder=new Vector4(58,58,58,58);
            t.SaveAndReimport();
        }
    }

    private static Sprite Sprite(string name)
    {
        var spr=AssetDatabase.LoadAssetAtPath<Sprite>(SpritesPath+name+".png");
        if(spr==null)throw new InvalidOperationException("Missing sprite: "+name);
        return spr;
    }
    private static RectTransform NewRect(Transform parent,string name)
    {
        var o=new GameObject(name,typeof(RectTransform));
        var r=o.GetComponent<RectTransform>();
        r.SetParent(parent,false);
        return r;
    }
    private static RectTransform Required(Transform parent,string name)
    {
        var t=parent.Find(name) as RectTransform;
        if(t==null)throw new InvalidOperationException("Missing review UI: "+parent.name+"/"+name);
        return t;
    }
    private static RectTransform Move(Transform from,Transform to,string name)
    {
        var t=Required(from,name);
        t.SetParent(to,false);
        return t;
    }
    private static void Fill(RectTransform r)
    {
        r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;
        r.offsetMin=Vector2.zero;r.offsetMax=Vector2.zero;
        r.localScale=Vector3.one;
    }
    private static void Place(RectTransform r,float x,float y,float width,float height)
    {
        r.anchorMin=r.anchorMax=new Vector2(0f,1f);
        r.pivot=new Vector2(0f,1f);
        r.anchoredPosition=new Vector2(x,-y);
        r.sizeDelta=new Vector2(width,height);
        r.localScale=Vector3.one;
        r.localRotation=Quaternion.identity;
    }
    private static Image ImageAt(Transform parent,string name,Sprite sprite,
        float x,float y,float width,float height)
    {
        var r=NewRect(parent,name);
        Place(r,x,y,width,height);
        var im=r.gameObject.AddComponent<Image>();
        im.sprite=sprite;im.color=Color.white;im.raycastTarget=false;
        return im;
    }
    private static void SetText(RectTransform r,float x,float y,float width,float height,
        float size,Color color,bool centered,float minSize,float maxSize)
    {
        Place(r,x,y,width,height);
        var t=r.GetComponent<TMP_Text>();
        if(t==null)throw new InvalidOperationException("Missing TMP on "+r.name);
        t.fontSize=size;
        t.enableAutoSizing=true;
        t.fontSizeMin=minSize;
        t.fontSizeMax=maxSize;
        t.alignment=centered?TextAlignmentOptions.Center:TextAlignmentOptions.MidlineLeft;
        t.color=color;
        t.margin=Vector4.zero;
        var fontPath = (r.name=="TitleText" || r.name=="TargetWordText" ||
            r.name=="ProgressText" || r.name=="LetterText" ||
            r.name=="ContinueButtonText" || r.name=="FeedbackTitleText" ||
            r.name=="SummaryTitleText" || r.name=="ScoreLabelText" ||
            r.name=="ScoreValueText" || r.name=="NeedsReviewLabelText")
            ? "Assets/Fonts/SPELLSTRIKE/Cinzel-Variable SDF.asset"
            : "Assets/Fonts/SPELLSTRIKE/Alegreya-Variable SDF.asset";
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        if(font!=null)t.font=font;
        t.textWrappingMode=TextWrappingModes.Normal;
        t.overflowMode=TextOverflowModes.Ellipsis;
        t.raycastTarget=false;
    }
}
