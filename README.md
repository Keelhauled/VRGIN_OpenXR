# VRGIN for Unity5.6.5
Eusth���쐬[VRGIN](https://github.com/Eusth/VRGIN)�ŁAUnity5.6.5�n�̃A�v���P�[�V������
�����_�����O��������肪���������ߑΉ��������́B

Mr. Eusth created [VRGIN] (https://github.com/Eusth/VRGIN), in Unity 5.6.5 series applications
Corresponded because there was a problem that rendering collapsed.

## �g�p���@ How to Use It
Eusth���쐬[VRGIN](https://github.com/Eusth/VRGIN)�L�ڂ̎g�p�菇���Q�Ƃ��������B
�܂��A�ǉ���VR���Ώۂ̃Q�[���Ƀp�b�`���Ă��K�v�ł��B
���L���Q�l�Ɏ��{���������B

Please refer to the use procedure described by Eusth [VRGIN] (https://github.com/Eusth/VRGIN).
In addition, it is necessary to apply patch to the game to be VRized additionally.
Please carry out referring to the following.

### VR���̂��߂̃p�b�`���� To the patch for VR conversion.
#### �K�v�ȃc�[�� Need tools.
- [UABE(Unity Asset Bundle Extractor) 2.2beta2](https://github.com/DerPopo/UABE/releases)

#### �p�b�`���ĕ��@ How to Patching
- �Q�[���C���X�g�[���t�H���_�ɂ���u�Q�[����\_Data/globalgamemanagers�v��UABE�ŊJ���B
- Path ID��11��Type��Build Settings�ƂȂ��Ă���s��I�����AExport Dump���s���B
- �쐬���ꂽ�_���v�t�@�C�����J���A22�s�ڂ�0 vector enabledVRDevices����0 int size = 0�܂ł����L�̂悤�ɏC���B

- Open "Game name\_Data/globalgamemanagers" in the game installation folder with UABE.
- Select the row whose Path ID column is 11 and whose Type is Build Settings, and perform Export Dump.
- Open the created dump file and fix from 0 vector enabledVRDevices on line 22 to 0 int size = 0 as shown below.

_�C���O preview fix_

0 vector enabledVRDevices  
 0 Array Array (0 items)  
  0 int size = 0  

_�C���� after fix_

0 vector enabledVRDevices  
  0 Array Array (2 items)  
   0 int size = 2  
   [0]  
    1 string data = "OpenVR"  
   [1]  
    1 string data = "None"  

\*1)0 vector buildTags �̑O�s�܂Œu��������/0 Replace the previous line of vector buildTags.  
\*2)"Oculus"�ɂ����Oculus�ł�������������܂��񂪁A���m�F�ł��B/If it is "Oculus" it may work with Oculus, but it is unconfirmed.  

- �ēxUABE��globalgamemanager���J���A�C�������_���v�t�@�C�����C���|�[�g���čX�V���Ă��������B  
- Open globalgamemanager again with UABE and import and update the modified dump file.
