// 获取滑动条、数字标签和输入框元素
const volumeInput_set = document.getElementById('volumeInput_set');
const sliderValue_set = document.getElementById('sliderValue_set');
const setValueInput_set = document.getElementById('setValueInput_set');     // 数字标签

const volumeInput_adjust = document.getElementById('volumeInput_adjust');
const sliderValue_adjust = document.getElementById('sliderValue_adjust');
const setValueInput_adjust = document.getElementById('setValueInput_adjust');   // 数字标签

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///
///     set radio
///
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// 更新滑动条和数字标签
function updateSliderValue(value) {
    // 根据值判断是否需要加上正负号
    if (value > 0) {
        sliderValue_set.textContent = `+${value}%`;
    } else if (value < 0) {
        sliderValue_set.textContent = `${value}%`;
    } else {
        sliderValue_set.textContent = `0%`;
    }
    // 显示数字标签
    sliderValue_set.style.display = 'block';
    // 获取数字标签的宽度
    const sliderValueWidth = sliderValue_set.offsetWidth;
    // 动态调整数字显示的位置
    const percentage = (value - volumeInput_set.min) / (volumeInput_set.max - volumeInput_set.min);
    const sliderWidth = volumeInput_set.offsetWidth;
    // 计算滑动点的位置
    const valuePosition = percentage * sliderWidth;
    // 通过调整 `left` 来确保数字标签水平居中，并加上额外的偏移量
    sliderValue_set.style.left = `${valuePosition - sliderValueWidth / 2 + 19}px`;
    // 更新输入框的值
    setValueInput_set.value = value;
}

// 监听滑动条的input事件
volumeInput_set.addEventListener('input', function () {
    const value = volumeInput_set.value;
    updateSliderValue(value);
});

// 监听输入框的input事件
setValueInput_set.addEventListener('input', function () {
    let value = setValueInput_set.value;

    // 如果输入框为空，或只含负号，则允许继续输入
    if (value === '-' || /^-?\d*$/.test(value)) {
        // 如果输入的值包含负号并且是数字，则允许
        if (value.length === 1 && value === '-') {
            return; // 负号是合法输入
        }
        // 如果输入的值是数字或负号后接数字，正常更新
        if (value === '' || !isNaN(value)) {
            // 限制输入的值在滑动条的范围内
            value = Math.max(Math.min(value, volumeInput_set.max), volumeInput_set.min);
            volumeInput_set.value = value;
            // $settings["volume"] = value;
            writeSetting("set", "volume", value);
            updateSliderValue(value);
        }
    } else {
        // 输入的内容不符合条件，清空输入框
        setValueInput_set.value = '';
    }
});

// 监听输入框失去焦点时隐藏数字标签
setValueInput_set.addEventListener('blur', function () {
    sliderValue_set.style.display = 'none';
});

// 监听滑动条的change事件（停止滑动时）
volumeInput_set.addEventListener('change', function () {
    // 停止滑动时隐藏数字标签
    sliderValue_set.style.display = 'none';
});

// 页面加载时，隐藏数字标签
window.addEventListener('load', function() {
    sliderValue_set.style.display = 'none';
});

// 初始更新显示
updateSliderValue(volumeInput_set.value);

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///
///     adjust radio
///
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// 更新滑动条和数字标签
function updateSliderValue1(value) {
    // 根据值判断是否需要加上正负号
    if (value > 0) {
        sliderValue_adjust.textContent = `+${value}%`;
    } else if (value < 0) {
        sliderValue_adjust.textContent = `${value}%`;
    } else {
        sliderValue_adjust.textContent = `0%`;
    }

    // 显示数字标签
    sliderValue_adjust.style.display = 'block';

    // 获取数字标签的宽度
    const sliderValueWidth = sliderValue_adjust.offsetWidth;

    // 动态调整数字显示的位置
    const percentage = (value - volumeInput_adjust.min) / (volumeInput_adjust.max - volumeInput_adjust.min);
    const sliderWidth = volumeInput_adjust.offsetWidth;

    // 计算滑动点的位置
    const valuePosition = percentage * sliderWidth;

    // 通过调整 `left` 来确保数字标签水平居中，并加上额外的偏移量
    sliderValue_adjust.style.left = `${valuePosition - sliderValueWidth / 2 + 19}px`;

    // 更新输入框的值
    setValueInput_adjust.value = value;
}

// 监听滑动条的input事件
volumeInput_adjust.addEventListener('input', function () {
    const value = volumeInput_adjust.value;
    updateSliderValue1(value);
});

// 监听输入框的input事件
setValueInput_adjust.addEventListener('input', function () {
    let value = setValueInput_adjust.value;

    // 如果输入框为空，或只含负号，则允许继续输入
    if (value === '-' || /^-?\d*$/.test(value)) {
        // 如果输入的值包含负号并且是数字，则允许
        if (value.length === 1 && value === '-') {
            return; // 负号是合法输入
        }
        // 如果输入的值是数字或负号后接数字，正常更新
        if (value === '' || !isNaN(value)) {
            // 限制输入的值在滑动条的范围内
            value = Math.max(Math.min(value, volumeInput_adjust.max), volumeInput_adjust.min);
            volumeInput_adjust.value = value;
            writeSetting("adjust", "volume", value);
            updateSliderValue1(value);
        }
    } else {
        // 输入的内容不符合条件，清空输入框
        setValueInput_adjust.value = '';
    }
});

// 监听输入框失去焦点时隐藏数字标签
setValueInput_adjust.addEventListener('blur', function () {
    sliderValue_adjust.style.display = 'none';
});

// 监听滑动条的change事件（停止滑动时）
volumeInput_adjust.addEventListener('change', function () {
    // 停止滑动时隐藏数字标签
    sliderValue_adjust.style.display = 'none';
});

// 页面加载时，隐藏数字标签
window.addEventListener('load', function() {
    sliderValue_adjust.style.display = 'none';
});

// 初始更新显示
updateSliderValue1(volumeInput_adjust.value);


///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///
///      radio select
///
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


