import csv
import os
import random

metadata_path = 'DataSet/data/metadata/metadata.csv'
electricity_path = 'DataSet/data/meters/cleaned/electricity_cleaned.csv'

out_dir = 'DataSet/subset'
os.makedirs(out_dir, exist_ok=True)

# 1. Read metadata
buildings = []
with open(metadata_path, 'r', encoding='utf-8') as f:
    reader = csv.DictReader(f)
    for row in reader:
        # Check if has sqm and electricity data is likely (we will double check with electricity header)
        if row.get('sqm') and row.get('primaryspaceusage'):
            buildings.append(row)

# Group by primaryspaceusage to get a diverse set
grouped = {}
for b in buildings:
    usage = b['primaryspaceusage']
    if usage not in grouped:
        grouped[usage] = []
    grouped[usage].append(b)

# Select one from each category until we hit 8
selected_buildings = []
keys = list(grouped.keys())
while len(selected_buildings) < 8 and keys:
    for k in keys:
         if len(selected_buildings) >= 8:
             break
         if grouped[k]:
             selected_buildings.append(grouped[k].pop(0))

# 2. Read electricity header to verify these exist in electricity_cleaned
selected_ids = [b['building_id'] for b in selected_buildings]

with open(electricity_path, 'r', encoding='utf-8') as f:
    header = next(csv.reader(f))
    
    # Filter selected_ids to those actually present
    valid_ids = [bid for bid in selected_ids if bid in header]
    print(f"Initial selected: {len(selected_ids)}, present in electricity: {len(valid_ids)}")
    
    # If we need more to hit 8
    if len(valid_ids) < 8:
        for b in buildings:
            if len(valid_ids) >= 8: break
            if b['building_id'] in header and b['building_id'] not in valid_ids:
                valid_ids.append(b['building_id'])
                selected_buildings.append(b)

# Keep only the valid selected buildings
final_buildings = [b for b in buildings if b['building_id'] in valid_ids][:8]
final_ids = [b['building_id'] for b in final_buildings]

# Write metadata subset
with open(os.path.join(out_dir, 'metadata_subset.csv'), 'w', newline='', encoding='utf-8') as f:
    if len(final_buildings) > 0:
        writer = csv.DictWriter(f, fieldnames=final_buildings[0].keys())
        writer.writeheader()
        writer.writerows(final_buildings)

# 3. Read electricity stream and write subset
with open(electricity_path, 'r', encoding='utf-8') as fin, open(os.path.join(out_dir, 'electricity_subset.csv'), 'w', newline='', encoding='utf-8') as fout:
    reader = csv.reader(fin)
    header = next(reader)
    
    # Find indices
    indices = [0] # timestamp
    for bid in final_ids:
        indices.append(header.index(bid))
        
    writer = csv.writer(fout)
    writer.writerow([header[i] for i in indices])
    
    # Take first 1000 rows (or all if specified, let's take all)
    for row in reader:
        writer.writerow([row[i] for i in indices])

print("Subset created successfully.")
print("Selected buildings:")
for b in final_buildings:
    print(f" - {b['building_id']} ({b['primaryspaceusage']}, {b['sqm']} sqm)")
