// AtlasIndexToUV.hlsl
// Shader Graph Custom Function Node용
// 정수 인덱스 하나로 아틀라스 격자에서 해당 칸의 UV를 계산한다.
//
// Custom Function Node 설정:
//   Type   : File
//   Source : 이 파일
//   Name   : AtlasIndexToUV   (Shader Graph가 자동으로 _float 접미사를 붙여 찾음)
//
// Inputs (그래프 슬롯 순서 — 이름이 아니라 순서로 바인딩됨, 반드시 아래 순서 그대로 추가할 것)
//   UV       : Vector2  ← UV0
//   Index    : Float    ← _AtlasIndex Property
//   GridSize : Vector2  ← _AtlasGridSize Property (열, 행 순서 = X=col수, Y=row수)
//
// Outputs
//   OutUV    : Vector2

void AtlasIndexToUV_float(
    float2 UV, float Index, float2 GridSize,
    out float2 OutUV)
{
    // Index: 0부터 시작. 좌상단(row0, col0)부터 가로로 채워나가는 순서.
    // (12.2절 쇼윈도 아틀라스와 동일 규칙 — index → column 그대로 매칭)
    float col = fmod(Index, GridSize.x);
    float row = floor(Index / GridSize.x);

    float2 tile = 1.0 / GridSize;

    // V(세로) 좌표는 텍스처 아래가 0, 위가 1이라
    // row가 커질수록(아래쪽 디자인일수록) offset.y가 작아져야 함.
    // 만약 결과가 상하 반전되어 보이면 아래 한 줄을
    //   float2 offset = float2(col * tile.x, row * tile.y);
    // 로 교체할 것 (프로젝트/텍스처 좌표계 차이 흡수용, 4장 트러블슈팅 참고)
    float2 offset = float2(col * tile.x, 1.0 - (row + 1.0) * tile.y);

    OutUV = UV * tile + offset;
}
