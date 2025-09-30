#include "PluginAPI.h"

#include "Model.h"
#include "Graphics.h"
#include "Server.h"
#include "Entity.h"
#include "Block.h"
#include "ExtMath.h"
#include "Game.h"


/*########################################################################################################################*
*------------------------------------------------------Dynamic imports----------------------------------------------------*
*#########################################################################################################################*/
// This is just to work around problems with normal dynamic linking
// - Importing CC_VAR forces mingw to use runtime relocation, which bloats the dll (twice the size) on Windows
// See the bottom of the file for the actual ugly importing
static void LoadSymbolsFromGame(void);
static struct _BlockLists* Blocks_;

static struct _GameData* Game_;

static struct _ModelsData* Models_;

static struct _ServerConnectionData* Server_;

static FP_String_AppendConst String_AppendConst_;


/*########################################################################################################################*
*--------------------------------------------------------Pony common------------------------------------------------------*
*#########################################################################################################################*/
static float hoofOffset;
static struct ModelTex pony_tex = { "pony.png" };

struct PonySet {
	struct ModelPart Head, Horn, LeftEar, RightEar, Hat, Neck, Snout, Torso, LeftWing, RightWing, Tail;
	struct ModelPart LeftLegFront, RightLegFront, LeftLegBack, RightLegBack, LeftWing2, RightWing2, Hair1, Hair2, Hair3;
};

static void Pony_LeftQuad(struct ModelPart* part, const struct BoxDesc* desc) {
	int sidesW = desc->sizeZ, bodyW = desc->sizeX, bodyH = desc->sizeY;
	float x1 = desc->x1, y1 = desc->y1, z1 = desc->z1;
	float x2 = desc->x2, y2 = desc->y2, z2 = desc->z2;

	int x = desc->texX, y = desc->texY;
	struct Model* m = Models_->Active;

	BoxDesc_XQuad(m, x + sidesW + bodyW, y + sidesW, sidesW, bodyH, z2, z1, y1, y2, x1, true); // left for real
	ModelPart_Init(part, m->index - 1 * 4, 1 * 4, desc->rotX, desc->rotY, desc->rotZ);
}

static void Pony_RightQuad(struct ModelPart* part, const struct BoxDesc* desc) {
	int sidesW = desc->sizeZ, bodyW = desc->sizeX, bodyH = desc->sizeY;
	float x1 = desc->x1, y1 = desc->y1, z1 = desc->z1;
	float x2 = desc->x2, y2 = desc->y2, z2 = desc->z2;

	int x = desc->texX, y = desc->texY;
	struct Model* m = Models_->Active;

	BoxDesc_XQuad(m, x, y + sidesW, sidesW, bodyH, z1, z2, y1, y2, x2, true); // right for real
	ModelPart_Init(part, m->index - 1 * 4, 1 * 4, desc->rotX, desc->rotY, desc->rotZ);
}

static void Pony_BuildWing(struct ModelPart* part, const struct BoxDesc* desc) {
	int sidesW = desc->sizeZ, bodyW = desc->sizeX, bodyH = desc->sizeY;
	float x1 = desc->x1, y1 = desc->y1, z1 = desc->z1;
	float x2 = desc->x2, y2 = desc->y2, z2 = desc->z2;

	int x = desc->texX, y = desc->texY;
	struct Model* m = Models_->Active;

	BoxDesc_YQuad(m, x + sidesW, y, bodyW, sidesW, x1, x2, z2, z1, y2, true); // top
	BoxDesc_YQuad(m, x + sidesW + bodyW, y, bodyW, sidesW, x2, x1, z2, z1, y1, false); // bottom
	ModelPart_Init(part, m->index - 2 * 4, 2 * 4, desc->rotX, desc->rotY, desc->rotZ);
}

static cc_bool IsAirBlock(BlockID b) { return Blocks_->Draw[b] != DRAW_GAS; }
static cc_bool NotAllBelowAir(struct Entity* p) {
	struct AABB bb;
	Vec3 size2; 
	
	Vec3_Mul1(&size2, &p->Size, 2);
	AABB_Make(&bb, &p->Position, &size2); // p->Size * 2

	bb.Max.Y = p->Position.Y - 0.5f;
	bb.Min.Y = p->Position.Y - 1.5f;
	return Entity_TouchesAny(&bb, IsAirBlock);
}
#define BoxDesc_ExtBounds(x1,y1,z1,x2,y2,z2) (x1-0.5f)/16.0f,(y1-0.5f)/16.0f,(z1-0.5f)/16.0f, (x2+0.5f)/16.0f,(y2+0.5f)/16.0f,(z2+0.5f)/16.0f

static void Pony_MakeBodyParts(struct PonySet* set) {
	float offsetX, offsetY, offsetZ;

	Pony_BuildWing(&set->RightWing2, &(struct BoxDesc) {
		BoxDesc_Tex(23, 39),
		BoxDesc_Dims(0, 0, 0, 13, 1, 8),
		BoxDesc_Bounds(-17, 12.5f, -8, -4, 13.5f, 0),
		BoxDesc_Rot(-4, 13, -4)
	});
	Pony_BuildWing(&set->LeftWing2, &(struct BoxDesc) {
		BoxDesc_Tex(23, 39),
		BoxDesc_Dims(0, 0, 0, 13, 1, 8),
		BoxDesc_Bounds(17, 12.5f, -8, 4, 13.5f, 0),
		BoxDesc_Rot(4, 13, -4)
	});

	BoxDesc_BuildBox(&set->LeftWing, &(struct BoxDesc) {
		BoxDesc_Tex(41, 17),
		BoxDesc_Dims(0, 0, 0, 1, 4, 7),
		BoxDesc_Bounds(-5, 11, -5, -4, 15, 2),
		BoxDesc_Rot(-4, 18, -5)
	});
	BoxDesc_BuildBox(&set->RightWing, &(struct BoxDesc) {
		BoxDesc_Tex(41, 17),
		BoxDesc_Dims(0, 0, 0, 1, 4, 7),
		BoxDesc_Bounds(5, 11, -5, 4, 15, 2),
		BoxDesc_Rot(-4, 18, -5)
	});

	BoxDesc_BuildBox(&set->Head, &(struct BoxDesc) {
		BoxDesc_Tex(0, 0),
		BoxDesc_Dims(0, 0, 0, 8, 8, 8),
		BoxDesc_Bounds(-4, 18, -10, 4, 26, -2),
		BoxDesc_Rot(0, 18, -5)
	});
	BoxDesc_BuildBox(&set->Hat, &(struct BoxDesc) {
		BoxDesc_Tex(32, 0),
		BoxDesc_Dims(0, 0, 0, 8, 8, 8),
		BoxDesc_ExtBounds(-4, 18, -10, 4, 26, -2),
		BoxDesc_Rot(0, 18, -5)
	});

	BoxDesc_BuildBox(&set->Snout, &(struct BoxDesc) {
		BoxDesc_Tex(0, 37),
		BoxDesc_Dims(0, 0, 0, 3, 2, 1),
		BoxDesc_Bounds(-1.5F, 18, -11, 1.5F, 20, -10),
		BoxDesc_Rot(0, 18, -5)
	});
	BoxDesc_BuildBox(&set->Horn, &(struct BoxDesc) {
		BoxDesc_Tex(24, 0),
		BoxDesc_Dims(0, 0, 0, 1, 5, 1),
		BoxDesc_Bounds(-0.5F, 26, -7, 0.5F, 31, -6),
		BoxDesc_Rot(0, 18, -5)
	});

	BoxDesc_BuildBox(&set->LeftEar, &(struct BoxDesc) {
		BoxDesc_Tex(0, 0),
		BoxDesc_Dims(0, 0, 0, 3, 4, 1),
		BoxDesc_Bounds(-4.5F, 24, -5, -1.5F, 28, -4),
		BoxDesc_Rot(0, 18, -5)
	});
	BoxDesc_BuildBox(&set->RightEar, &(struct BoxDesc) {
		BoxDesc_Tex(0, 0),
		BoxDesc_Dims(0, 0, 0, 3, 4, 1),
		BoxDesc_Bounds(4.5F, 24, -5, 1.5F, 28, -4),
		BoxDesc_Rot(0, 18, -5)
	});

	BoxDesc_BuildBox(&set->Torso, &(struct BoxDesc) {
		BoxDesc_Tex(0, 16),
		BoxDesc_Dims(0, 0, 0, 8, 8, 12),
		BoxDesc_Bounds(-4, 8, -6, 4, 16, 6),
		BoxDesc_Rot(0, 12, -2)
	});

	Pony_RightQuad(&set->Tail, &(struct BoxDesc) {
		BoxDesc_Tex(41, 32),
		BoxDesc_Dims(0, 0, 0, 2, 16, 16),
		BoxDesc_Bounds(-2, 3, 6, 0, 19, 22),
		BoxDesc_Rot(-1, 14, 6)
	});

	BoxDesc_BuildBox(&set->Neck, &(struct BoxDesc) {
		BoxDesc_Tex(5, 36),
		BoxDesc_Dims(0, 0, 0, 4, 7, 5),
		BoxDesc_Bounds(-2, 12.5F, -7, 2, 19.5F, -2),
		BoxDesc_Rot(-4, 18, -5)
	});

	BoxDesc_BuildBox(&set->LeftLegFront, &(struct BoxDesc) {
		BoxDesc_Tex(28, 16),
		BoxDesc_Dims(0, 0, 0, 3, 8, 3),
		BoxDesc_Bounds(0.5F, 0 + hoofOffset, -5.5F, 3.5F, 8, -2.5F),
		BoxDesc_Rot(-4, 8, -4)
	});
	BoxDesc_BuildBox(&set->RightLegFront, &(struct BoxDesc) {
		BoxDesc_Tex(28, 16),
		BoxDesc_Dims(0, 0, 0, 3, 8, 3),
		BoxDesc_Bounds(-0.5F, 0 + hoofOffset, -5.5F, -3.5F, 8, -2.5F),
		BoxDesc_Rot(-4, 8, -4)
	});

	BoxDesc_BuildBox(&set->LeftLegBack, &(struct BoxDesc) {
		BoxDesc_Tex(0, 16),
		BoxDesc_Dims(0, 0, 0, 3, 8, 3),
		BoxDesc_Bounds(1, 0 + hoofOffset, 4, 4, 8, 7),
		BoxDesc_Rot(-3, 8, 5)
	});
	BoxDesc_BuildBox(&set->RightLegBack, &(struct BoxDesc) {
		BoxDesc_Tex(0, 16),
		BoxDesc_Dims(0, 0, 0, 3, 8, 3),
		BoxDesc_Bounds(-1, 0 + hoofOffset, 4, -4, 8, 7),
		BoxDesc_Rot(3, 8, 5)
	});

	offsetX = -2; offsetY = 17; offsetZ = -15;
	Pony_RightQuad(&set->Hair1, &(struct BoxDesc) {
		BoxDesc_Tex(41, 49), //middle
		BoxDesc_Dims(0, 0, 0, 2, 16, 16),
		BoxDesc_Bounds(offsetX, offsetY, offsetZ, 2 + offsetX, 16 + offsetY, 16 + offsetZ),
		BoxDesc_Rot(0, 18, -5)
	});

	offsetX = 2; offsetY = 12; offsetZ = -14;
	Pony_RightQuad(&set->Hair2, &(struct BoxDesc) {
		BoxDesc_Tex(5, 49), //right side
		BoxDesc_Dims(0, 0, 0, 2, 16, 16),
		BoxDesc_ExtBounds(offsetX, offsetY, offsetZ, 2 + offsetX, 16 + offsetY, 16 + offsetZ),
		BoxDesc_Rot(0, 18, -5)
	});

	offsetX = -4; offsetY = 12; offsetZ = -14;
	Pony_LeftQuad(&set->Hair3, &(struct BoxDesc) {
		BoxDesc_Tex(5, 66), //left side
		BoxDesc_Dims(0, 0, 0, 2, 16, 16),
		BoxDesc_ExtBounds(offsetX, offsetY, offsetZ, 2 + offsetX, 16 + offsetY, 16 + offsetZ),
		BoxDesc_Rot(0, 18, -5)
	});
}

static float Pony_GetHeadTilt(struct Entity* p) {
	float headTilt = -p->Pitch;
	if (headTilt >= -180) {
		headTilt *= 0.5f;
	} else {
		headTilt = -180 + headTilt * 0.5f; // -360 + (360 + headTilt) * 0.5f
	}
	return headTilt;
}

static void Pony_DrawBodyParts(struct Entity* p, struct PonySet* set) {
	PackedCol base  = Models_->Cols[0];
	PackedCol shade = PackedCol_Scale(base, PACKEDCOL_SHADE_Z);
	float headTilt  = Pony_GetHeadTilt(p);

	Models_->Cols[1] = Models_->Cols[2] = Models_->Cols[3] = Models_->Cols[4] = Models_->Cols[5] = shade;
	Model_ApplyTexture(p);
	Models_->uScale = 1 / 128.0f; Models_->vScale = 1 / 128.0f;

	Gfx_SetAlphaTest(false);
	Model_DrawRotate(headTilt * MATH_DEG2RAD, 0, 0, &set->Head, true);
	Model_DrawPart(&set->Torso);
	Model_UpdateVB();
	Gfx_SetAlphaTest(true);

	if (NotAllBelowAir(p)) {
		Model_DrawPart(&set->LeftWing);
		Model_DrawPart(&set->RightWing);
	} else {
		const float legMax = 80 * MATH_DEG2RAD; // stolen from animatedcomponent
		float wingZRot = -(float)(Math_Cos(Game_->Time * 10) * legMax);
		Model_DrawRotate(0, 0, -wingZRot, &set->RightWing2, false);
		Model_DrawRotate(0, 0, wingZRot, &set->LeftWing2, false);
	}

	Model_DrawRotate(headTilt * MATH_DEG2RAD, 0, 0, &set->Hat, true);
	Model_DrawRotate(headTilt * MATH_DEG2RAD - 0.4F, 0, 0, &set->Horn, true);
	Model_DrawRotate(headTilt * MATH_DEG2RAD, 0, 0, &set->LeftEar, true);
	Model_DrawRotate(headTilt * MATH_DEG2RAD, 0, 0, &set->RightEar, true);
	Model_DrawRotate(headTilt * MATH_DEG2RAD, 0, 0, &set->Snout, true);
	Model_DrawRotate(-0.4F, 0, 0, &set->Neck, false);
	const float tailTilt = 0.05F;
	const float tailRoll = 0.1F;

	Models_->Cols[0] = shade;
	Model_DrawRotate(0, 0, 0, &set->Tail, false);
	Model_DrawRotate(0, tailTilt, -tailRoll, &set->Tail, false);
	Model_DrawRotate(0, -tailTilt, tailRoll, &set->Tail, false);
	Models_->Cols[0] = base;

	if (!NotAllBelowAir(p)) {
		Model_DrawPart(&set->LeftLegFront);
		Model_DrawPart(&set->RightLegFront);
		Model_DrawPart(&set->LeftLegBack);
		Model_DrawPart(&set->RightLegBack);
	} else {
		Model_DrawRotate(p->Anim.LeftLegX  * 0.5F, 0, 0, &set->LeftLegFront,  false);
		Model_DrawRotate(p->Anim.RightLegX * 0.5F, 0, 0, &set->RightLegFront, false);
		Model_DrawRotate(p->Anim.RightLegX * 0.5F, 0, 0, &set->LeftLegBack,   false);
		Model_DrawRotate(p->Anim.LeftLegX  * 0.5F, 0, 0, &set->RightLegBack,  false);
	}

	Models_->Cols[0] = shade;
	Model_DrawRotate(headTilt * MATH_DEG2RAD, -0.2f, 0, &set->Hair1, true); //-0.2f
	Model_DrawRotate(headTilt * MATH_DEG2RAD, -0,    0, &set->Hair2, true); //-0.2f
	Model_DrawRotate(headTilt * MATH_DEG2RAD, -0,    0, &set->Hair3, true); //-0.2f

	Model_UpdateVB();
}


/*########################################################################################################################*
*---------------------------------------------------------PonyModel-------------------------------------------------------*
*#########################################################################################################################*/
static struct PonySet normSet;

static void PonyModel_MakeParts(void) {
	hoofOffset = 0;
	Pony_MakeBodyParts(&normSet);
}

static void PonyModel_Draw(struct Entity* p) {
	Pony_DrawBodyParts(p, &normSet);
}

static float PonyModel_GetNameY(struct Entity* e) { return 28/16.0f; }
static float PonyModel_GetEyeY(struct Entity* e)  { return 21/16.0f; }
static void PonyModel_GetSize(struct Entity* e) {
	e->Size = (Vec3) { 8.6f/16.0f, 26.1f/16.0f, 8.6f/16.0f };
}

static void PonyModel_GetBounds(struct Entity* e) {
	e->ModelAABB = (struct AABB) { -5/16.0f, 0, -14/16.0f, 5/16.0f, 28/16.0f, 9/16.0f };
}

static struct ModelVertex pony_vertices[MODEL_BOX_VERTICES * 20];
static struct Model pony_model = { "pony", pony_vertices, &pony_tex,
	PonyModel_MakeParts, PonyModel_Draw,
	PonyModel_GetNameY,  PonyModel_GetEyeY,
	PonyModel_GetSize,   PonyModel_GetBounds
};

static struct Model* PonyModel_GetInstance(void) {
	Model_Init(&pony_model);
	pony_model.usesHumanSkin = true;
	return &pony_model;
}


/*########################################################################################################################*
*---------------------------------------------------------TallPonyModel---------------------------------------------------*
*#########################################################################################################################*/
#define TALL_OFFSET 4
static struct PonySet tallSet;

static void TallPonyModel_MakeParts(void) {
	hoofOffset = -TALL_OFFSET;
	Pony_MakeBodyParts(&tallSet);
}

static void TallPonyModel_Draw(struct Entity* p) {
	Pony_DrawBodyParts(p, &tallSet);
}

static float TallPonyModel_GetEyeY(struct Entity* e) {	
	return PonyModel_GetEyeY(e) + (TALL_OFFSET / 16.0f); 
}

static void TallPonyModel_GetTransform(struct Entity* e, Vec3 pos, struct Matrix* m) {
	pos.Y += (TALL_OFFSET / 16.0f) * e->ModelScale.Y;
	Entity_GetTransform(e, pos, e->ModelScale, m);
}

static struct ModelVertex tall_vertices[MODEL_BOX_VERTICES * 20];
static struct Model tall_model = { "tallpony", tall_vertices, &pony_tex,
	TallPonyModel_MakeParts, TallPonyModel_Draw,
	PonyModel_GetNameY,      TallPonyModel_GetEyeY,
	PonyModel_GetSize,       PonyModel_GetBounds
};

static struct Model* TallPonyModel_GetInstance(void) {
	Model_Init(&tall_model);
	tall_model.usesHumanSkin = true;
	tall_model.GetTransform  = TallPonyModel_GetTransform;
	return &tall_model;
}


/*########################################################################################################################*
*---------------------------------------------------------PonySitModel----------------------------------------------------*
*#########################################################################################################################*/
static struct PonySet sitSet;

static void PonySitModel_MakeParts(void) {
	hoofOffset = 0;
	Pony_MakeBodyParts(&sitSet);

	float frontLegOffsetY = 2.0f;
	float frontLegOffsetZ = -1.0f;

	BoxDesc_BuildBox(&sitSet.LeftLegFront, &(struct BoxDesc) {
		BoxDesc_Tex(28, 16),
		BoxDesc_Dims(0, 0, 0, 3, 8, 3),
		BoxDesc_Bounds(0.5F, 0 + hoofOffset + frontLegOffsetY, -5.5F + frontLegOffsetZ, 3.5F, 8 + frontLegOffsetY, -2.5F + frontLegOffsetZ),
		BoxDesc_Rot(-4, 8 + frontLegOffsetY, -4 + frontLegOffsetZ)
	});
	BoxDesc_BuildBox(&sitSet.RightLegFront, &(struct BoxDesc) {
		BoxDesc_Tex(28, 16),
		BoxDesc_Dims(0, 0, 0, 3, 8, 3),
		BoxDesc_Bounds(-0.5F, 0 + hoofOffset + frontLegOffsetY, -5.5F + frontLegOffsetZ, -3.5F, 8 + frontLegOffsetY, -2.5F + frontLegOffsetZ),
		BoxDesc_Rot(-4, 8 + frontLegOffsetY, -4 + frontLegOffsetZ)
	});


	float backLegOffsetX = 1.0f;
	float backLegOffsetY = -3.0f;
	float backLegOffsetZ = -3.0f;

	BoxDesc_BuildBox(&sitSet.LeftLegBack, &(struct BoxDesc) {
		BoxDesc_Tex(0, 16),
		BoxDesc_Dims(0, 0, 0, 3, 8, 3),
		BoxDesc_Bounds(1 + backLegOffsetX, 0 + hoofOffset + backLegOffsetY, 4 + backLegOffsetZ, 4 + backLegOffsetX, 8 + backLegOffsetY, 7 + backLegOffsetZ),
		BoxDesc_Rot(3 + backLegOffsetX, 8 + backLegOffsetY, 5 + backLegOffsetZ)
	});
	BoxDesc_BuildBox(&sitSet.RightLegBack, &(struct BoxDesc) {
		BoxDesc_Tex(0, 16),
		BoxDesc_Dims(0, 0, 0, 3, 8, 3),
		BoxDesc_Bounds(-1 - backLegOffsetX, 0 + hoofOffset + backLegOffsetY, 4 + backLegOffsetZ, -4 - backLegOffsetX, 8 + backLegOffsetY, 7 + backLegOffsetZ),
		BoxDesc_Rot(-3 - backLegOffsetX, 8 + backLegOffsetY, 5 + backLegOffsetZ)
	});


	float tailOffsetY = -6.0f;
	float tailOffsetZ = -0.8f;

	Pony_RightQuad(&sitSet.Tail, &(struct BoxDesc) {
		BoxDesc_Tex(41, 32),
		BoxDesc_Dims(0, 0, 0, 2, 16, 16),
		BoxDesc_Bounds(-2, 3 + tailOffsetY, 6 + tailOffsetZ, 0, 19 + tailOffsetY, 22 + tailOffsetZ),
		BoxDesc_Rot(-1, 14 + tailOffsetY, 6 + tailOffsetZ)
	});


	BoxDesc_BuildBox(&sitSet.LeftWing, &(struct BoxDesc) {
		BoxDesc_Tex(41, 17),
		BoxDesc_Dims(0, 0, 0, 1, 4, 7),
		BoxDesc_Bounds(-5, 11 + tailOffsetY, -5 + tailOffsetZ, -4, 15 + tailOffsetY, 2 + tailOffsetZ),
		BoxDesc_Rot(-1, 14 + tailOffsetY, 6 + tailOffsetZ)
	});
	BoxDesc_BuildBox(&sitSet.RightWing, &(struct BoxDesc) {
		BoxDesc_Tex(41, 17),
		BoxDesc_Dims(0, 0, 0, 1, 4, 7),
		BoxDesc_Bounds(5, 11 + tailOffsetY, -5 + tailOffsetZ, 4, 15 + tailOffsetY, 2 + tailOffsetZ),
		BoxDesc_Rot(-1, 14 + tailOffsetY, 6 + tailOffsetZ)
	});
}

static float tempRotate = 0.77f;
static float legRotY = 0;
static float legRotZ = 0.3f;
static void PonySitModel_Draw(struct Entity* p) {
	PackedCol base  = Models_->Cols[0];
	PackedCol shade = PackedCol_Scale(base, PACKEDCOL_SHADE_Z);
	float headTilt  = Pony_GetHeadTilt(p);
	struct PonySet* set = &sitSet;

	Models_->Cols[1] = Models_->Cols[2] = Models_->Cols[3] = Models_->Cols[4] = Models_->Cols[5] = shade;
	Model_ApplyTexture(p);
	Models_->uScale = 1 / 128.0f; Models_->vScale = 1 / 128.0f;

	Gfx_SetAlphaTest(false);
	Model_DrawRotate(headTilt * MATH_DEG2RAD, 0, 0, &set->Head,  true);
	Model_DrawRotate(tempRotate,              0, 0, &set->Torso, false);
	Model_UpdateVB();
	Gfx_SetAlphaTest(true);

	Model_DrawRotate(tempRotate, 0, 0, &set->LeftWing,  false);
	Model_DrawRotate(tempRotate, 0, 0, &set->RightWing, false);

	Model_DrawRotate(headTilt * MATH_DEG2RAD,        0, 0, &set->Hat,      true);
	Model_DrawRotate(headTilt * MATH_DEG2RAD - 0.4F, 0, 0, &set->Horn,     true);
	Model_DrawRotate(headTilt * MATH_DEG2RAD,        0, 0, &set->LeftEar,  true);
	Model_DrawRotate(headTilt * MATH_DEG2RAD,        0, 0, &set->RightEar, true);
	Model_DrawRotate(headTilt * MATH_DEG2RAD,        0, 0, &set->Snout,    true);
	Model_DrawRotate(-0.4F,                          0, 0, &set->Neck,     false);
	const float tailTilt = 0.05F;
	const float tailRoll = 0.1F;

	Models_->Cols[0] = shade;
	Model_DrawRotate(tempRotate,         0,         0, &set->Tail, false);
	Model_DrawRotate(tempRotate,  tailTilt, -tailRoll, &set->Tail, false);
	Model_DrawRotate(tempRotate, -tailTilt,  tailRoll, &set->Tail, false);
	Models_->Cols[0] = base;

	Model_DrawRotate(0.4F, 0, 0, &set->LeftLegFront,  false);
	Model_DrawRotate(0.4F, 0, 0, &set->RightLegFront, false);

	Model_DrawRotate(90 * MATH_DEG2RAD, legRotY,  legRotZ, &set->LeftLegBack,  false);
	Model_DrawRotate(90 * MATH_DEG2RAD, legRotY, -legRotZ, &set->RightLegBack, false);

	Models_->Cols[0] = shade;
	Model_DrawRotate(headTilt * MATH_DEG2RAD, -0.2f, 0, &set->Hair1, true); //-0.2f
	Model_DrawRotate(headTilt * MATH_DEG2RAD,     0, 0, &set->Hair2, true); //-0.2f
	Model_DrawRotate(headTilt * MATH_DEG2RAD,     0, 0, &set->Hair3, true); //-0.2f

	Model_UpdateVB();
}

#define SIT_OFFSET -3
static float PonySitModel_GetEyeY(struct Entity* e) { return (21 + SIT_OFFSET) / 16.0f; }

static void PonySitModel_GetTransform(struct Entity* e, Vec3 pos, struct Matrix* m) {
	pos.Y += (SIT_OFFSET / 16.0f) * e->ModelScale.Y;
	Entity_GetTransform(e, pos, e->ModelScale, m);
}

static struct ModelVertex sit_vertices[MODEL_BOX_VERTICES * (20 + 7)];
static struct Model sit_model = { "ponysit", sit_vertices, &pony_tex,
	PonySitModel_MakeParts, PonySitModel_Draw,
	PonyModel_GetNameY,     PonySitModel_GetEyeY,
	PonyModel_GetSize,      PonyModel_GetBounds
};

static struct Model* PonySitModel_GetInstance(void) {
	Model_Init(&sit_model);
	sit_model.usesHumanSkin = true;
	sit_model.GetTransform = PonySitModel_GetTransform;
	return &sit_model;
}

/*########################################################################################################################*
*----------------------------------------------------------Plugin---------------------------------------------------------*
*#########################################################################################################################*/

static struct VertexTextured large_vertices[24 * 20];
static void Pony_Init(void) {
	LoadSymbolsFromGame();
	Model_RegisterTexture(&pony_tex);
	Model_Register(PonyModel_GetInstance());
	Model_Register(TallPonyModel_GetInstance());
	Model_Register(PonySitModel_GetInstance());

	// Recreate the modelcache VB to be bigger
	Gfx_DeleteVb(&Models_->Vb);
	Models_->Vertices    = large_vertices;
	Models_->MaxVertices = 24 * 20;
	Models_->Vb = Gfx_CreateDynamicVb(VERTEX_FORMAT_TEXTURED, Models_->MaxVertices);

	String_AppendConst_(&Server_->AppName, " + Ponies v2.1");
}

PLUGIN_EXPORT int Plugin_ApiVersion = 1;
PLUGIN_EXPORT struct IGameComponent Plugin_Component = {
	Pony_Init /* Init */
};


/*########################################################################################################################*
*------------------------------------------------------Dynamic loading----------------------------------------------------*
*#########################################################################################################################*/
#define QUOTE(x) #x

#ifdef CC_BUILD_WIN
#define WIN32_LEAN_AND_MEAN
#define NOSERVICE
#define NOMCX
#define NOIME
#include <windows.h>
#define LoadSymbol(name) name ## _ = GetProcAddress(GetModuleHandleA(NULL), QUOTE(name))
#else
#define _GNU_SOURCE
#include <dlfcn.h>
#define LoadSymbol(name) name ## _ = dlsym(RTLD_DEFAULT, QUOTE(name))
#endif

static void LoadSymbolsFromGame(void) {
	LoadSymbol(Blocks);
	
	LoadSymbol(Game);
	
	LoadSymbol(Models);
	
	LoadSymbol(Server); 

	LoadSymbol(String_AppendConst);
}
