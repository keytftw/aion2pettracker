$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$pets=Get-Content (Join-Path $root 'data/pet-catalog.json') -Raw|ConvertFrom-Json
# Read public pages only. Keep short acquisition facts, never articles or artwork.
$results=$pets | ForEach-Object -Parallel {
    $pet=$_
    $slug=($pet.Name.ToLowerInvariant() -replace '[^a-z0-9]+','-').Trim('-')
    $url='https://wikily.gg/aion-2/pets/'+$slug
    try {
        $html=(Invoke-WebRequest -Uri $url -TimeoutSec 30).Content
        $title=[Net.WebUtility]::HtmlDecode([regex]::Match($html,'<title>(.*?)</title>').Groups[1].Value)
        if($title -ne ($pet.Name+' Pet | AION 2 Wiki')){return [pscustomobject]@{Id=$pet.Id;Verified=$false;Reason='Page name mismatch'}}
        $section=[regex]::Match($html,'How to get it.*?</section>','Singleline').Value
        if(!$section){return [pscustomobject]@{Id=$pet.Id;Verified=$false;Reason='No acquisition section'}}
        $facts=@([regex]::Matches($section,'<span class="ext-gwe-card-list-title">(.*?)</span>(?:<span class="ext-gwe-card-list-subtitle">(.*?)</span>)?','Singleline')|ForEach-Object {
            $text=[Net.WebUtility]::HtmlDecode([regex]::Replace(($_.Groups[1].Value+' · '+$_.Groups[2].Value),'<[^>]+>',' '))
            ([regex]::Replace($text,'\s+',' ')).Trim()
        } | Where-Object {$_} | Select-Object -Unique -First 20)
        [pscustomobject]@{Id=$pet.Id;Verified=($facts.Count -gt 0);Source=$url;Checked='2026-10-14';Facts=$facts}
    }catch{[pscustomobject]@{Id=$pet.Id;Verified=$false;Reason='Page unavailable'}}
} -ThrottleLimit 2
$results|Sort-Object Id|ConvertTo-Json -Depth 5|Set-Content (Join-Path $root '.build/pet-source-research.json') -Encoding utf8
$results|Group-Object Verified|Select-Object Name,Count
