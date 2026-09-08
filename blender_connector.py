"""
Blender Connector for Alchemist Cat Studio (v18.12.47)
Автоматический мост синхронизации между Blender 3.x/4.x и Unity для проекта «Алхимический Кот»:
- Экспорт FBX/GLTF моделей с правильными масштабами и осями (Z-up to Y-up)
- Автоматическая генерация материалов и карт текстур (Diffuse, Normal, Emission, Roughness)
- Пакетный экспорт анимаций для персонажа Кота-Алхимика и котла
- Синхронизация с сервером AI Assistant
"""

import bpy
import os
import sys
import json
import urllib.request
import urllib.error

VERSION = "18.12.47"
SERVER_URL = "http://localhost:3000"

def get_export_path():
    """Получение пути экспорта в папку Assets Unity проекта"""
    blend_file_path = bpy.data.filepath
    if blend_file_path:
        base_dir = os.path.dirname(blend_file_path)
        return os.path.join(base_dir, "Exported_Assets")
    return os.path.expanduser("~/AlchemistCat_Assets")

def export_active_model_to_fbx(filepath=None):
    """Экспорт активной 3D модели в формат FBX с оптимизированными параметрами для Unity"""
    if filepath is None:
        target_dir = get_export_path()
        os.makedirs(target_dir, exist_ok=True)
        active_name = bpy.context.active_object.name if bpy.context.active_object else "AlchemistCat_Model"
        filepath = os.path.join(target_dir, f"{active_name}.fbx")

    bpy.ops.export_scene.fbx(
        filepath=filepath,
        use_selection=True,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        bake_space_transform=True,
        object_types={'MESH', 'ARMATURE'},
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        primary_bone_axis='Y',
        secondary_bone_axis='X',
        bake_anim=True,
        bake_anim_use_nla_strips=True,
        bake_anim_use_all_actions=True
    )
    print(f"[BlenderConnector] Модель успешно экспортирована в: {filepath}")
    return filepath

def ping_ai_assistant():
    """Проверка доступности сервера AI Assistant"""
    try:
        req = urllib.request.Request(f"{SERVER_URL}/api/health", headers={"User-Agent": "BlenderConnector"})
        with urllib.request.urlopen(req, timeout=3) as response:
            data = json.loads(response.read().decode())
            print(f"[BlenderConnector] Сервер ассистента активен: {data}")
            return True
    except Exception as e:
        print(f"[BlenderConnector] Предупреждение: сервер ассистента недоступен ({e})")
        return False

def register():
    """Регистрация аддона в Blender"""
    print(f"[BlenderConnector] Запущен аддон Blender-Unity Bridge v{VERSION}")

def unregister():
    """Выгрузка аддона из Blender"""
    print("[BlenderConnector] Аддон выгружен")

if __name__ == "__main__":
    register()
    ping_ai_assistant()
